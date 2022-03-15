using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    [CollectionAgeLimit(Minutes = 2)]
    public class MonsterGrain : WandererGrain, IMonsterGrain
    {
        const string MONSTER_IMAGE_PATH = "/img/monster.png";
        const string WINK_IMAGE_PATH = "/img/wink.png";

        public MonsterGrain(
            [PersistentState("wanderer", "wandererStorage")] IPersistentState<Wanderer> wanderer, 
            ILogger<WandererGrain> logger) : base(wanderer, logger)
        {
        }

        public async Task Eat(IWanderGrain grain)
        {
            var deadWanderer = await grain.GetWanderer();
            grain.Dispose();
            await SpeedUp(4);
        }

        public override async Task SetInfo(Wanderer wanderer)
        {
            Wanderer.State.AvatarImageUrl = MONSTER_IMAGE_PATH;
            await base.SetInfo(wanderer);
        }

        ITileGrain _currentTileGrain;
        public override async Task SetLocation(ITileGrain tileGrain)
        {
            Wanderer.State.AvatarImageUrl = MONSTER_IMAGE_PATH;
            if(_currentTileGrain != null)
            {
                await EatEverythingHere(_currentTileGrain);
            }
            _currentTileGrain = tileGrain;
            await base.SetLocation(tileGrain);
        }

        private async Task EatEverythingHere(ITileGrain tileGrain)
        {
            // eat the first wanderer you see, if there are any
            var tile = await tileGrain.GetTile();
            var theUnfortunate = tile.WanderersHere.Where(x => x.Name != this.GetPrimaryKeyString()).ToList();
            theUnfortunate.ForEach(async _ =>
            {
                Wanderer.State.AvatarImageUrl = WINK_IMAGE_PATH;

                var unfortunateGrain = GrainFactory.GetGrain<IWanderGrain>(_.Name, typeof(WandererGrain).FullName);
                await Eat(unfortunateGrain);
                await tileGrain.Leaves(await unfortunateGrain.GetWanderer());
                unfortunateGrain.Dispose();
            });
        }
    }
}
