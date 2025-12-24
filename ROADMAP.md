Battlegrounds
A strategic auto-battler card game combining the drafting mechanics of popular card battlers with the tactical depth and lore of popular TTRPGs. Built in Unity (Universal Render Pipeline).

Project Overview
    Engine: Unity 2022+ (LTS)
    Language: C#
    Platform: PC / Mobile (Cross-platform architecture)
    
Development Roadmap

    Phase 1: The Core Loop (Current)
        [x] Recruit Phase (Economy & Buying)
        [x] Combat Phase (Auto-battler simulation)
        [x] Board Persistence (Units return after battle)
        [x] Damage Calculation (Hero takes damage)
        [ ] Death Loop (Saving Throws & Unconscious State)
        
    Phase 2: The "Fun" (Mechanics)
        [ ] Ability System (Infrastructure for Triggers)
        [ ] Battlecries (On Play)
        [ ] Deathrattles (On Death)
        [ ] Auras (Passive Buffs)
        [ ] Hero Powers (Active Buttons)
        [ ] Visual Polish (Impact particles, sound effects)
        
    Phase 3: The Structure (PvP Framework)
        [ ] Lobby System (Simulated)
        [ ] Create 7 "Bot" opponents with random names.
        [ ] Track HP for all 8 players on a scoreboard.
        [ ] Matchmaking logic (Who fights whom?).
        [ ] Ghost Data[ ] Save player board state to JSON.
        [ ] Load enemy board state from JSON.
        
    Phase 4: The Content (PvE Modes)
        [ ] Dungeon Run (Goal Oriented)
        [ ] Map Screen / Node Selection.
        [ ] Boss Encounters.
        [ ] Endless Mode (Survival)
        [ ] Difficulty Scaling Algorithm.
        [ ] Reward intervals (Every 30 rounds).