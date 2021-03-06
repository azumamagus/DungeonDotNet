using MvcRDMG.Core.Abstractions.Services;
using MvcRDMG.Core.Abstractions.Services.Models;
using MvcRDMG.Core.Domain;
using MvcRDMG.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MvcRDMG.Seed
{
    public class ContextSeedData
    {
        private readonly Context _context;
        private readonly IDungeonGenerator _generator;
        private readonly IUserService _userService;
        public ContextSeedData(IDungeonGenerator generator, IUserService userService, Context context)
        {
            _context = context;
            _generator = generator;
            _userService = userService;
        }
        public async Task SeedDataAsync()
        {
            if (!_context.Users.Any())
            {
                await _userService.CreateAsync(new UserModel { Password = "testPassword12345", Username = "TestUser" });

                var model = new OptionModel()
                {
                    DungeonName = "Test1",
                    Created = DateTime.UtcNow,
                    ItemsRarity = 1,
                    DeadEnd = true,
                    DungeonDifficulty = 1,
                    DungeonSize = 20,
                    MonsterType = "any",
                    PartyLevel = 4,
                    PartySize = 4,
                    TrapPercent = 20,
                    RoamingPercent = 10,
                    TreasureValue = 1,
                    RoomDensity = 10,
                    RoomSize = 20,
                    Corridor = true
                };

                _generator.Generate(model);

                var sd = model.SavedDungeons.ToList();

                var dungeon = new Option()
                {
                    UserId = 1,
                    DungeonName = "Test1",
                    Created = DateTime.UtcNow,
                    ItemsRarity = 1,
                    DeadEnd = true,
                    DungeonDifficulty = 1,
                    DungeonSize = 20,
                    MonsterType = "any",
                    PartyLevel = 4,
                    PartySize = 4,
                    TrapPercent = 20,
                    RoamingPercent = 10,
                    TreasureValue = 1,
                    RoomDensity = 10,
                    RoomSize = 20,
                    Corridor = true,
                    SavedDungeons = new List<SavedDungeon>()
                    {
                        new SavedDungeon() {DungeonTiles = sd[0].DungeonTiles ,RoomDescription = sd[0].RoomDescription, TrapDescription = sd[0].TrapDescription, RoamingMonsterDescription = sd[0].RoamingMonsterDescription}
                    }
                };
                _context.Options.Add(dungeon);
                _context.SavedDungeons.AddRange(dungeon.SavedDungeons);

                var model2 = new OptionModel()
                {
                    DungeonName = "Test2",
                    Created = DateTime.UtcNow,
                    ItemsRarity = 1,
                    DeadEnd = true,
                    DungeonDifficulty = 1,
                    DungeonSize = 25,
                    MonsterType = "any",
                    PartyLevel = 4,
                    PartySize = 4,
                    TrapPercent = 20,
                    RoamingPercent = 0,
                    TreasureValue = 1,
                    RoomDensity = 10,
                    RoomSize = 20,
                    Corridor = false
                };
                _generator.Generate(model2);

                var sd2 = model2.SavedDungeons.ToList();

                var dungeon2 = new Option()
                {
                    UserId = 1,
                    DungeonName = "Test2",
                    Created = DateTime.UtcNow,
                    ItemsRarity = 1,
                    DeadEnd = true,
                    DungeonDifficulty = 1,
                    DungeonSize = 25,
                    MonsterType = "any",
                    PartyLevel = 4,
                    PartySize = 4,
                    TrapPercent = 20,
                    RoamingPercent = 0,
                    TreasureValue = 1,
                    RoomDensity = 10,
                    RoomSize = 20,
                    Corridor = false,
                    SavedDungeons = new List<SavedDungeon>()
                    {
                        new SavedDungeon() {DungeonTiles = sd2[0].DungeonTiles ,RoomDescription = sd2[0].RoomDescription, TrapDescription = sd2[0].TrapDescription, RoamingMonsterDescription = sd2[0].RoamingMonsterDescription}
                    }
                };
                _context.Options.Add(dungeon2);
                _context.SavedDungeons.AddRange(dungeon2.SavedDungeons);

                await _context.SaveChangesAsync();
            }
        }
    }
}