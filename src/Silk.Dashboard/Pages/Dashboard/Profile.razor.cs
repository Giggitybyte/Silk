using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AspNetCore.Components;
using Silk.Dashboard.Services;

namespace Silk.Dashboard.Pages.Dashboard
{
    /* Todo: Change handling of GuildViews which are not UserGuilds (i.e AllGuilds: will cause the ManageGuild page to error) */
    public partial class Profile : ComponentBase
    {
        [Inject] private DiscordRestClientService RestClientService { get; set; }

        private bool _showJoinedGuilds;
        
        private IReadOnlyList<DiscordGuild> _joinedGuilds;
        private IReadOnlyList<DiscordGuild> _ownedGuilds;

        protected override async Task OnInitializedAsync()
        {
            _joinedGuilds = await RestClientService.GetAllGuildsAsync();
            _ownedGuilds = RestClientService.FilterGuildsByPermission(_joinedGuilds, Permissions.ManageGuild);
        }

        private string CurrentUserAvatar => RestClientService.RestClient.CurrentUser.GetAvatarUrl(ImageFormat.Auto);
        private string CurrentUserName => RestClientService.RestClient.CurrentUser.Username;
        private string HeaderViewGreeting => $"Hello, {CurrentUserName}";

        private void ToggleJoinedGuildsVisibility() => _showJoinedGuilds = !_showJoinedGuilds;
    }
}