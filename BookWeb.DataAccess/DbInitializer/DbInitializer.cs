using bookweb.Data;
using BookWeb.Models;
using BookWeb.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;

namespace BookWeb.DataAccess.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;
        private readonly string _webRootPath;

        public DbInitializer(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
            _webRootPath = env.WebRootPath;
        }

        public void Initialize()
        {
            // Apply pending migrations
            try
            {
                if (_db.Database.GetPendingMigrations().Any())
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception) { }

            // Create roles if they don't exist
            if (!_roleManager.RoleExistsAsync(SD.Role_Admin).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Editor)).GetAwaiter().GetResult();

                // Create admin user
                _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "admin@bookweb.com",
                    Email = "admin@bookweb.com",
                    Name = "Admin User",
                    PhoneNumber = "1234567890",
                    EmailConfirmed = true
                }, "Admin123*").GetAwaiter().GetResult();

                var user = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "admin@bookweb.com");
                if (user != null)
                {
                    _userManager.AddToRoleAsync(user, SD.Role_Admin).GetAwaiter().GetResult();
                }
            }

            // Seed books from CSV if no products exist
            if (!_db.Products.Any())
            {
                SeedBooksFromCsv();
            }
        }

        private void SeedBooksFromCsv()
        {
            var csvPath = Path.Combine(_webRootPath, "data", "Books.csv");
            if (!File.Exists(csvPath)) return;

            var genreToCategoryMap = BuildGenreCategoryMap();
            var categories = new Dictionary<string, Category>();
            int displayOrder = 1;

            var books = new List<(string title, string author, int pages, string genre, string description, string publishedDate, string publisher, string language, string thumbnail)>();

            using (var parser = new TextFieldParser(csvPath))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;

                // Skip header
                if (!parser.EndOfData) parser.ReadFields();

                while (!parser.EndOfData)
                {
                    try
                    {
                        var fields = parser.ReadFields();
                        if (fields == null || fields.Length < 11) continue;

                        var title = fields[0].Trim();
                        var author = fields[1].Trim();
                        int.TryParse(fields[2].Trim(), out int pages);
                        var genre = fields[3].Trim();
                        var description = fields[4].Trim();
                        var publishedDate = fields[5].Trim();
                        var publisher = fields[6].Trim();
                        var language = fields[7].Trim();
                        var thumbnail = fields[10].Trim();

                        if (string.IsNullOrEmpty(title)) continue;

                        books.Add((title, author, pages, genre, description, publishedDate, publisher, language, thumbnail));
                    }
                    catch { continue; }
                }
            }

            // Create categories
            foreach (var book in books)
            {
                var categoryName = MapGenreToCategory(book.genre, genreToCategoryMap);
                if (!categories.ContainsKey(categoryName))
                {
                    var category = new Category
                    {
                        Name = categoryName,
                        DisplayOrder = displayOrder++
                    };
                    _db.Categories.Add(category);
                    categories[categoryName] = category;
                }
            }
            _db.SaveChanges();

            // Create products
            var random = new Random(42); // Fixed seed for reproducibility
            foreach (var book in books)
            {
                var categoryName = MapGenreToCategory(book.genre, genreToCategoryMap);
                var category = categories[categoryName];

                var listPrice = random.Next(15, 80);
                var price = Math.Round(listPrice * 0.85, 2);
                var price50 = Math.Round(listPrice * 0.75, 2);
                var price100 = Math.Round(listPrice * 0.65, 2);

                var product = new Product
                {
                    Title = book.title.Length > 200 ? book.title[..200] : book.title,
                    Author = string.IsNullOrEmpty(book.author) ? "Unknown Author" : book.author,
                    Description = string.IsNullOrEmpty(book.description) ? "No description available" : book.description,
                    Pages = book.pages,
                    Publisher = string.IsNullOrEmpty(book.publisher) ? null : book.publisher,
                    Language = string.IsNullOrEmpty(book.language) ? null : book.language,
                    PublishedDate = string.IsNullOrEmpty(book.publishedDate) ? null : book.publishedDate,
                    ListPrice = listPrice,
                    Price = price,
                    Price50 = price50,
                    Price100 = price100,
                    CategoryId = category.Id,
                    ImageUrl = string.IsNullOrEmpty(book.thumbnail) ? null : book.thumbnail
                };
                _db.Products.Add(product);
            }
            _db.SaveChanges();
        }

        private static string MapGenreToCategory(string genre, Dictionary<string, string> map)
        {
            if (string.IsNullOrWhiteSpace(genre) || genre == "Unknown Genre")
                return "Other";

            // Check exact match first
            if (map.TryGetValue(genre, out var mapped))
                return mapped;

            // Check partial match
            var lowerGenre = genre.ToLower();
            if (lowerGenre.Contains("fiction") || lowerGenre.Contains("stories") || lowerGenre.Contains("novel"))
                return "Fiction";
            if (lowerGenre.Contains("history") || lowerGenre.Contains("war"))
                return "History";
            if (lowerGenre.Contains("science") && !lowerGenre.Contains("fiction"))
                return "Science & Nature";
            if (lowerGenre.Contains("law") || lowerGenre.Contains("legislation") || lowerGenre.Contains("politic"))
                return "Law & Politics";
            if (lowerGenre.Contains("education") || lowerGenre.Contains("study") || lowerGenre.Contains("teaching"))
                return "Education";
            if (lowerGenre.Contains("cook") || lowerGenre.Contains("food"))
                return "Cooking & Food";
            if (lowerGenre.Contains("religio") || lowerGenre.Contains("bible") || lowerGenre.Contains("church"))
                return "Religion & Spirituality";
            if (lowerGenre.Contains("medical") || lowerGenre.Contains("health") || lowerGenre.Contains("medicine"))
                return "Health & Medicine";
            if (lowerGenre.Contains("art") || lowerGenre.Contains("photo") || lowerGenre.Contains("architect"))
                return "Art & Architecture";
            if (lowerGenre.Contains("music") || lowerGenre.Contains("drama") || lowerGenre.Contains("perform"))
                return "Music & Performing Arts";
            if (lowerGenre.Contains("poetry") || lowerGenre.Contains("poem") || lowerGenre.Contains("sonnets"))
                return "Poetry";
            if (lowerGenre.Contains("literary") || lowerGenre.Contains("literature") || lowerGenre.Contains("criticism"))
                return "Literary Criticism";
            if (lowerGenre.Contains("business") || lowerGenre.Contains("econom") || lowerGenre.Contains("finance") || lowerGenre.Contains("account"))
                return "Business & Economics";
            if (lowerGenre.Contains("computer") || lowerGenre.Contains("technology") || lowerGenre.Contains("engineering"))
                return "Technology";
            if (lowerGenre.Contains("travel") || lowerGenre.Contains("tourism"))
                return "Travel";
            if (lowerGenre.Contains("sport") || lowerGenre.Contains("recreation") || lowerGenre.Contains("game"))
                return "Sports & Games";
            if (lowerGenre.Contains("children") || lowerGenre.Contains("juvenile") || lowerGenre.Contains("young adult"))
                return "Children & Young Adult";
            if (lowerGenre.Contains("biograph") || lowerGenre.Contains("autobiograph"))
                return "Biography";
            if (lowerGenre.Contains("psycholog") || lowerGenre.Contains("self-help") || lowerGenre.Contains("personal"))
                return "Self-Help & Psychology";
            if (lowerGenre.Contains("reference") || lowerGenre.Contains("library") || lowerGenre.Contains("catalog") || lowerGenre.Contains("index") || lowerGenre.Contains("subject heading"))
                return "Reference";
            if (lowerGenre.Contains("philosoph") || lowerGenre.Contains("ethics"))
                return "Philosophy";

            return "Other";
        }

        private static Dictionary<string, string> BuildGenreCategoryMap()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Fiction"] = "Fiction",
                ["Science fiction"] = "Science Fiction & Fantasy",
                ["Fantasy fiction"] = "Science Fiction & Fantasy",
                ["Fantasy fiction, American"] = "Science Fiction & Fantasy",
                ["Fantasy fiction, English"] = "Science Fiction & Fantasy",
                ["Dystopias in literature"] = "Science Fiction & Fantasy",
                ["Supernatural"] = "Science Fiction & Fantasy",
                ["Adventure stories"] = "Fiction",
                ["Crime"] = "Fiction",
                ["History"] = "History",
                ["Biography & Autobiography"] = "Biography",
                ["Biography"] = "Biography",
                ["Christian biography"] = "Biography",
                ["Science"] = "Science & Nature",
                ["Nature"] = "Science & Nature",
                ["Botany"] = "Science & Nature",
                ["Evolution"] = "Science & Nature",
                ["Meteorology"] = "Science & Nature",
                ["Astronomy"] = "Science & Nature",
                ["Mathematics"] = "Science & Nature",
                ["Astronautics"] = "Science & Nature",
                ["Technology & Engineering"] = "Technology",
                ["Computers"] = "Technology",
                ["Telecommunication"] = "Technology",
                ["Business & Economics"] = "Business & Economics",
                ["Finance"] = "Business & Economics",
                ["Investing"] = "Business & Economics",
                ["Accounting"] = "Business & Economics",
                ["Entrepreneurship"] = "Business & Economics",
                ["Health & Fitness"] = "Health & Medicine",
                ["Medical"] = "Health & Medicine",
                ["Medicine"] = "Health & Medicine",
                ["Mental health"] = "Health & Medicine",
                ["Public health"] = "Health & Medicine",
                ["Aging"] = "Health & Medicine",
                ["Education"] = "Education",
                ["Study Aids"] = "Education",
                ["Activity programs in education"] = "Education",
                ["Art"] = "Art & Architecture",
                ["Architecture"] = "Art & Architecture",
                ["Photography"] = "Art & Architecture",
                ["Religion"] = "Religion & Spirituality",
                ["Bible"] = "Religion & Spirituality",
                ["God"] = "Religion & Spirituality",
                ["Latter Day Saints"] = "Religion & Spirituality",
                ["Missions"] = "Religion & Spirituality",
                ["Law"] = "Law & Politics",
                ["Political Science"] = "Law & Politics",
                ["Administrative law"] = "Law & Politics",
                ["Administrative procedure"] = "Law & Politics",
                ["Administrative agencies"] = "Law & Politics",
                ["Legislative hearings"] = "Law & Politics",
                ["Poetry"] = "Poetry",
                ["American poetry"] = "Poetry",
                ["English poetry"] = "Poetry",
                ["Poetry, Modern"] = "Poetry",
                ["Literary Criticism"] = "Literary Criticism",
                ["Literary Collections"] = "Literary Criticism",
                ["Literature"] = "Literary Criticism",
                ["American literature"] = "Literary Criticism",
                ["English literature"] = "Literary Criticism",
                ["Language Arts & Disciplines"] = "Literary Criticism",
                ["Juvenile Fiction"] = "Children & Young Adult",
                ["Juvenile Nonfiction"] = "Children & Young Adult",
                ["Young Adult Fiction"] = "Children & Young Adult",
                ["Young Adult Nonfiction"] = "Children & Young Adult",
                ["Children's stories"] = "Children & Young Adult",
                ["Children's stories, English"] = "Children & Young Adult",
                ["Sports & Recreation"] = "Sports & Games",
                ["Sports"] = "Sports & Games",
                ["Dance"] = "Music & Performing Arts",
                ["Games & Activities"] = "Sports & Games",
                ["Cooking"] = "Cooking & Food",
                ["Cooking, American"] = "Cooking & Food",
                ["Cooking, Indic"] = "Cooking & Food",
                ["Cooking for military personnel"] = "Cooking & Food",
                ["Food"] = "Cooking & Food",
                ["Travel"] = "Travel",
                ["Tourism"] = "Travel",
                ["Reference"] = "Reference",
                ["Encyclopedias and dictionaries"] = "Reference",
                ["Self-Help"] = "Self-Help & Psychology",
                ["Psychology"] = "Self-Help & Psychology",
                ["Personal Finance"] = "Self-Help & Psychology",
                ["Humor"] = "Humor & Comics",
                ["Comics & Graphic Novels"] = "Humor & Comics",
                ["Music"] = "Music & Performing Arts",
                ["Performing Arts"] = "Music & Performing Arts",
                ["Drama"] = "Music & Performing Arts",
                ["English drama"] = "Music & Performing Arts",
                ["American drama"] = "Music & Performing Arts",
                ["French drama"] = "Music & Performing Arts",
                ["Philosophy"] = "Philosophy",
                ["Philosophy, Ancient"] = "Philosophy",
                ["Ethics"] = "Philosophy",
                ["Aesthetics"] = "Philosophy",
                ["Body, Mind & Spirit"] = "Religion & Spirituality",
                ["Social Science"] = "Social Science",
                ["Antiques & Collectibles"] = "Other",
                ["Crafts & Hobbies"] = "Other",
                ["Foreign Language Study"] = "Education",
                ["Transportation"] = "Technology",
                ["Transportation, Automotive"] = "Technology",
                ["Family & Relationships"] = "Self-Help & Psychology",
                ["Psychopharmacology"] = "Health & Medicine",
                ["Psychology, Pathological"] = "Self-Help & Psychology",
                ["Environmental protection"] = "Science & Nature",
                ["Coastal ecology"] = "Science & Nature",
                ["Agriculture"] = "Science & Nature",
                ["Agroforestry"] = "Science & Nature",
            };
        }
    }
}
