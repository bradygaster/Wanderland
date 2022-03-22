using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    [CollectionAgeLimit(Minutes = 2)]
    public class MonsterGrain : WandererGrain, IMonsterGrain
    {
        const string MONSTER_LEFT_1 = "/img/monster-left-1.png";
        const string MONSTER_LEFT_2 = "/img/monster-left-2.png";
        const string MONSTER_RIGHT_1 = "/img/monster-right-1.png";
        const string MONSTER_RIGHT_2 = "/img/monster-right-2.png";
        const string MONSTER_UP_1 = "/img/monster-up-1.png";
        const string MONSTER_UP_2 = "/img/monster-up-2.png";
        const string MONSTER_DOWN_1 = "/img/monster-down-1.png";
        const string MONSTER_DOWN_2 = "/img/monster-down-2.png";
        const string MONSTER = "/img/monster.png";

        public MonsterGrain(
            [PersistentState("wanderer", "wandererStorage")] IPersistentState<Wanderer> wanderer, 
            ILogger<WandererGrain> logger) : base(wanderer, logger)
        {
        }
        public override Task GoNorth()
        {
            Wanderer.State.AvatarImageUrl = Wanderer.State.AvatarImageUrl == MONSTER_UP_1 ? MONSTER_UP_2 : MONSTER_UP_1;
            return base.GoNorth();
        }

        public override Task GoWest()
        {
            Wanderer.State.AvatarImageUrl = Wanderer.State.AvatarImageUrl == MONSTER_LEFT_1 ? MONSTER_LEFT_2 : MONSTER_LEFT_1;
            return base.GoWest();
        }

        public override Task GoSouth()
        {
            Wanderer.State.AvatarImageUrl = Wanderer.State.AvatarImageUrl == MONSTER_DOWN_1 ? MONSTER_DOWN_2 : MONSTER_DOWN_1;
            return base.GoSouth();
        }

        public override Task GoEast()
        {
            Wanderer.State.AvatarImageUrl = Wanderer.State.AvatarImageUrl == MONSTER_RIGHT_1 ? MONSTER_RIGHT_2 : MONSTER_RIGHT_1;
            return base.GoEast();
        }

        public async Task Eat(IWandererGrain grain)
        {
            Wanderer.State.AvatarImageUrl = MONSTER;
            var deadWanderer = await grain.GetWanderer();
            grain.Dispose();
            await SpeedUp(4);
        }

        ITileGrain _currentTileGrain;
        public override async Task SetLocation(ITileGrain tileGrain)
        {
            await EatEverythingHere(_currentTileGrain);
            _currentTileGrain = tileGrain;
            await base.SetLocation(tileGrain);
            await EatEverythingHere(tileGrain);
        }

        async Task<Monster> IMonsterGrain.GetWanderer()
        {
            return await base.GetWanderer() as Monster;
        }

        public async Task SetInfo(Monster wanderer)
        {
            await base.SetInfo(wanderer);
        }

        private async Task EatEverythingHere(ITileGrain tileGrain)
        {
            if (tileGrain == null) return;
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
