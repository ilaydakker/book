using bookweb.Data;
using BookWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace bookweb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public ChatController(ApplicationDbContext db, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index(int? conversationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var conversations = await _db.ChatConversations
                .Where(c => c.ApplicationUserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.Conversations = conversations;

            if (conversationId.HasValue)
            {
                var conversation = await _db.ChatConversations
                    .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                    .FirstOrDefaultAsync(c => c.Id == conversationId && c.ApplicationUserId == userId);

                ViewBag.ActiveConversation = conversation;
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> NewConversation()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var conversation = new ChatConversation
            {
                ApplicationUserId = userId!,
                Title = "New Chat",
                CreatedAt = DateTime.UtcNow
            };

            _db.ChatConversations.Add(conversation);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, conversationId = conversation.Id });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConversation([FromBody] DeleteConversationRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var conversation = await _db.ChatConversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId && c.ApplicationUserId == userId);

            if (conversation == null)
                return NotFound();

            _db.ChatMessages.RemoveRange(conversation.Messages);
            _db.ChatConversations.Remove(conversation);
            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
                return BadRequest(new { success = false, message = "Message is required." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var apiKey = _configuration["DeepSeek:ApiKey"];
            var model = _configuration["DeepSeek:Model"] ?? "deepseek-chat";
            var baseUrl = _configuration["DeepSeek:BaseUrl"] ?? "https://api.deepseek.com";

            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_DEEPSEEK_API_KEY_HERE")
                return Ok(new { success = false, message = "AI chat is not configured. Please set up the DeepSeek API key." });

            // Get or create conversation
            ChatConversation? conversation = null;
            if (request.ConversationId.HasValue)
            {
                conversation = await _db.ChatConversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.Id == request.ConversationId && c.ApplicationUserId == userId);
            }

            if (conversation == null)
            {
                conversation = new ChatConversation
                {
                    ApplicationUserId = userId!,
                    Title = request.Message.Length > 50 ? request.Message[..50] + "..." : request.Message,
                    CreatedAt = DateTime.UtcNow
                };
                _db.ChatConversations.Add(conversation);
                await _db.SaveChangesAsync();
            }

            // Save user message
            var userMsg = new ChatMessageEntity
            {
                ConversationId = conversation.Id,
                Role = "user",
                Content = request.Message,
                CreatedAt = DateTime.UtcNow
            };
            _db.ChatMessages.Add(userMsg);
            await _db.SaveChangesAsync();

            // Update title from first message if it's "New Chat"
            if (conversation.Title == "New Chat")
            {
                conversation.Title = request.Message.Length > 50 ? request.Message[..50] + "..." : request.Message;
                await _db.SaveChangesAsync();
            }

            // Search for relevant books
            var bookContext = await GetRelevantBooksAsync(request.Message);

            var systemPrompt = @"You are a helpful book assistant for BookWeb, an online bookstore and reading tracker. 
You help users discover books, get recommendations, and answer questions about books in our catalog.
Be concise, friendly, and helpful. If you recommend books, mention their title and author.
Only recommend books from the provided catalog context. If you don't have relevant books in the context, say so politely.

Here are relevant books from our catalog:
" + bookContext;

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            // Add conversation history from DB
            var history = await _db.ChatMessages
                .Where(m => m.ConversationId == conversation.Id)
                .OrderByDescending(m => m.CreatedAt)
                .Take(10)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            foreach (var h in history)
            {
                messages.Add(new { role = h.Role, content = h.Content });
            }

            var requestBody = new
            {
                model = model,
                messages = messages,
                max_tokens = 500,
                temperature = 0.7
            };

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{baseUrl}/v1/chat/completions", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return Ok(new { success = false, message = "Sorry, I'm having trouble connecting right now. Please try again later.", conversationId = conversation.Id });
                }

                using var doc = JsonDocument.Parse(responseBody);
                var reply = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                // Save assistant message
                var assistantMsg = new ChatMessageEntity
                {
                    ConversationId = conversation.Id,
                    Role = "assistant",
                    Content = reply ?? "",
                    CreatedAt = DateTime.UtcNow
                };
                _db.ChatMessages.Add(assistantMsg);
                await _db.SaveChangesAsync();

                return Ok(new { success = true, message = reply, conversationId = conversation.Id, title = conversation.Title });
            }
            catch
            {
                return Ok(new { success = false, message = "Sorry, something went wrong. Please try again.", conversationId = conversation.Id });
            }
        }

        private async Task<string> GetRelevantBooksAsync(string query)
        {
            var queryLower = query.ToLower();

            // Strip common stop words so only meaningful terms remain
            var stopWords = new HashSet<string> { "give", "me", "some", "a", "an", "the", "books", "about", "on", "for", "with", "from", "book", "recommend", "find", "show", "list", "have", "you", "do", "can", "any", "are", "there", "what", "which", "tell", "want", "like", "looking", "suggest", "please", "just", "get", "want", "need" };
            var words = queryLower
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2 && !stopWords.Contains(w))
                .ToArray();

            if (!words.Any())
            {
                var random = await _db.Products.Include(p => p.Category).OrderBy(p => Guid.NewGuid()).Take(10).ToListAsync();
                return BuildBookContext(random);
            }

            // 1. Try category match first (e.g. "history", "fiction", "science")
            var categories = await _db.Categories.ToListAsync();
            var matchedCategory = categories.FirstOrDefault(c =>
                words.Any(w => c.Name.ToLower().Contains(w) || w.Contains(c.Name.ToLower())));

            if (matchedCategory != null)
            {
                var catBooks = await _db.Products
                    .Include(p => p.Category)
                    .Where(p => p.CategoryId == matchedCategory.Id)
                    .OrderBy(p => Guid.NewGuid())
                    .Take(15)
                    .ToListAsync();
                return BuildBookContext(catBooks);
            }

            // 2. Fall back: search per word separately (EF can translate single Contains)
            var results = new List<Product>();
            foreach (var word in words.Take(3))
            {
                var w = word;
                var matched = await _db.Products
                    .Include(p => p.Category)
                    .Where(p => p.Title.ToLower().Contains(w) ||
                                p.Author.ToLower().Contains(w) ||
                                p.Description.ToLower().Contains(w))
                    .Take(10)
                    .ToListAsync();
                results.AddRange(matched);
            }

            var distinct = results.DistinctBy(p => p.Id).Take(15).ToList();
            if (distinct.Any())
                return BuildBookContext(distinct);

            var fallback = await _db.Products.Include(p => p.Category).OrderBy(p => Guid.NewGuid()).Take(10).ToListAsync();
            return BuildBookContext(fallback);
        }

        private static string BuildBookContext(List<Product> books)
        {
            var sb = new StringBuilder();
            foreach (var book in books)
            {
                sb.AppendLine($"- \"{book.Title}\" by {book.Author} | Category: {book.Category?.Name} | Pages: {book.Pages} | Price: ${book.Price:F2}");
                if (!string.IsNullOrEmpty(book.Description) && book.Description != "No description available")
                {
                    var desc = book.Description.Length > 150 ? book.Description[..150] + "..." : book.Description;
                    sb.AppendLine($"  Description: {desc}");
                }
            }
            return sb.ToString();
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public int? ConversationId { get; set; }
    }

    public class DeleteConversationRequest
    {
        public int ConversationId { get; set; }
    }
}
