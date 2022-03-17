using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    [CollectionAgeLimit(Minutes = 2)]
    public class MonsterGrain : WandererGrain, IMonsterGrain
    {
        const string MONSTER_LEFT = "/img/monster-left.png";
        const string MONSTER_RIGHT = "/img/monster-right.png";
        const string MONSTER_UP = "/img/monster-up.png";
        const string MONSTER_DOWN = "/img/monster-down.png";
        const string MONSTER = "/img/monster.png";

        public MonsterGrain(
            [PersistentState("wanderer", "wandererStorage")] IPersistentState<Wanderer> wanderer, 
            ILogger<WandererGrain> logger) : base(wanderer, logger)
        {
        }
        public override Task GoNorth()
        {
            Wanderer.State.AvatarImageUrl = MONSTER_UP;
            return base.GoNorth();
        }

        public override Task GoWest()
        {
            Wanderer.State.AvatarImageUrl = MONSTER_LEFT;
            return base.GoWest();
        }

        public override Task GoSouth()
        {
            Wanderer.State.AvatarImageUrl = MONSTER_DOWN;
            return base.GoSouth();
        }

        public override Task GoEast()
        {
            Wanderer.State.AvatarImageUrl = MONSTER_RIGHT;
            return base.GoEast();
        }

        public async Task Eat(IWandererGrain grain)
        {
            Wanderer.State.AvatarImageUrl = MONSTER;
            var deadWanderer = await grain.GetWanderer();
            grain.Dispose();
            await SpeedUp(4);
        }

        public override async Task SetInfo(Wanderer wanderer)
        {
            await base.SetInfo(wanderer);
        }

        ITileGrain _currentTileGrain;
        public override async Task SetLocation(ITileGrain tileGrain)
        {
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
            var theUnfortunate = tile.ThingsHere.Where(x => x.Name != this.GetPrimaryKeyString()).ToList();
            theUnfortunate.ForEach(async _ =>
            {
                var unfortunateGrain = GrainFactory.GetGrain<IWandererGrain>(_.Name, typeof(WandererGrain).FullName);
                await Eat(unfortunateGrain);
                await tileGrain.Leaves(await unfortunateGrain.GetWanderer());
                unfortunateGrain.Dispose();
            });
        }
    }
}
