using BookWeb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookWeb.DataAccess.DbInitializer
{
    public interface IDbInitializer
    {
        void Initialize();
    }
}
