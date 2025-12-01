using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NomadGisMobile.Models;

namespace NomadGisMobile.Services
{
    public class AchievementsService
    {
        private readonly ApiClient _api;

        public AchievementsService(ApiClient api)
        {
            _api = api;
        }

        /// <summary>
        /// Все ачивки (GET /api/v1/achievements)
        /// </summary>
        public Task<List<AchievementResponse>?> GetAllAsync()
        {
            return _api.GetAsync<List<AchievementResponse>>("/api/v1/achievements");
        }

        /// <summary>
        /// Ачивки текущего пользователя (GET /api/v1/profile/my-achievements).
        /// Нужен выставленный Bearer-токен в ApiClient.
        /// </summary>
        public Task<List<AchievementResponse>?> GetMyAsync()
        {
            return _api.GetAsync<List<AchievementResponse>>("/api/v1/profile/my-achievements");
        }
    }
}

