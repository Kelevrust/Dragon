Battlegrounds

A strategic auto-battler card game combining the drafting mechanics of popular card battlers with the tactical depth and lore of popular TTRPGs. Built in Unity (Universal Render Pipeline).

Project Overview

Engine: Unity 2022+ (LTS)
Language: C#
Platform: PC / Mobile (Cross-platform architecture)

Development Roadmap

Phase 1: The Core Loop (Complete)

    [x] Recruit Phase (Economy & Buying)
    [x] Combat Phase (Auto-battler simulation)
    [x] Board Persistence (Units return after battle)
    [x] Damage Calculation (Hero takes damage)
    [x] Death Loop (Saving Throws & Unconscious State)

Phase 1.5: Advanced Economy & Flow (Next Up)

    [ ] Hand System: Buying a unit moves it to a "Hand" zone first. Drag from Hand to Board to play.
    [ ] Tavern Refresh: Reroll the shop selection for 1 Gold.
    [ ] Tavern Tier System:
        [ ] Pay Gold to upgrade Tavern Tier.
        [ ] Higher tiers unlock better units in the shop.

Phase 2: The "Fun" (Mechanics)

    [x] Ability System Core: (Infrastructure Built)
    [x] Deathrattles (Token Spawning)
    [ ] Battlecries (On Play Buffs)
    [ ] Auras (Passive Buffs)
    [ ] Hero Powers (Active Buttons)
    [ ] Visual Polish (Impact particles, sound effects)

Phase 2.5: System Features & Polish (Backlog)

    [ ] Settings Menu:
        [ ] Resolution & Window Mode options.
        [ ] Graphics Quality (Shadows, Texture Resolution).
        [ ] Audio Mixer (Master, Music, SFX volume).
        [ ] Drag & Drop Purchasing: Fix Raycast logic for direct buy/sell.
        [ ] UI Scaling: Ensure text boxes handle large numbers/names gracefully.

Phase 3: The Structure (PvP Framework)

    [ ] Lobby System (Simulated)
    [ ] Create 7 "Bot" opponents with random names.
    [ ] Track HP for all 8 players on a scoreboard.
    [ ] Matchmaking logic (Who fights whom?).
    [ ] Ghost Data
    [ ] Save player board state to JSON.
    [ ] Load enemy board state from JSON.

Phase 4: The Content (PvE Modes)

    [ ] Dungeon Run (Goal Oriented)
    [ ] Map Screen / Node Selection.
    [ ] Boss Encounters.
    [ ] Endless Mode (Survival)
    [ ] Difficulty Scaling Algorithm.
    [ ] Reward intervals (Every 30 rounds).