Battlegrounds (Project Grimm)

A strategic auto-battler card game combining the drafting mechanics of popular card battlers with the tactical depth of roguelite RPGs.

Visual Identity: Neon Noir.
Reference: The Wolf Among Us, Sin City, Cyberpunk Edgerunners.
Palette: Deep blacks, hard outlines, neon accents (Cyan/Magenta).
Setting: Modern urban decay overlaid with ancient folklore.

Project Overview
Engine: Unity 2022+ (LTS)
Language: C#
Platform: PC / Android (Initial Target)
Backend: Microsoft Azure PlayFab (LiveOps, Identity, Data)

Development Roadmap

Phase 1: The Core Loop (Complete)

[x] Recruit Phase (Economy & Buying)
[x] Combat Phase (Auto-battler simulation)
[x] Board Persistence (Units return after battle)
[x] Damage Calculation (Hero takes damage)
[x] Death Loop (Saving Throws & Unconscious State)

Phase 1.5: Advanced Economy & Flow (Complete)

[x] Hand System: Buying a unit moves it to a "Hand" zone first. Drag from Hand to Board to play.
[x] Tavern Refresh: Reroll the shop selection for 1 Gold.
[x] Tavern Tier System:
[x] Pay Gold to upgrade Tavern Tier.
[x] Higher tiers unlock better units in the shop.
[x] Discount logic (Upgrade cost reduces by 1 per turn).
[x] Freeze Shop: Lock current shop cards for next turn.
[x] Card Shoe (Pool): Finite unit copies to prevent force-building.
[ ] Interest System: Gain +1 Gold for every 10 Gold saved (Cap at +5).
[ ] Bank System: Ability to store gold between turns.

Phase 2: The "Fun" (Mechanics)

[x] Ability System Core: (Infrastructure Built)
[x] Deathrattles (Token Spawning)
[x] Battlecries (On Play Buffs)
[x] Auras (Passive Buffs)
[x] Scavenge (On Ally Death)
[x] Synergy (On Ally Play)
[x] Hero Powers (Active Buttons & Logic)
[ ] Visual Polish:
[ ] "Neon Noir" Shader/UI skinning.
[x] Impact particles & sound effects.

Phase 2.5: System Features & Polish (Complete)

[x] Unit Tooltips: Mouseover to see detailed stats, keywords, and buff sources.
[x] Settings Menu:
[x] Resolution & Window Mode options.
[x] Graphics Quality (Auto-Detect).
[x] Audio Mixer (Master, Music, SFX volume).
[x] Passive Analytics: Integrate basic event tracking (Round End, Win/Loss, Economy).
[x] Drag & Drop Purchasing: Fix Raycast logic for direct buy/sell.

Phase 3: The Structure (PvP Framework)

Goal: Move from local simulation to cloud-based asynchronous multiplayer.

[ ] Backend: Azure PlayFab Integration:
[ ] Install PlayFab SDK.
[ ] Implement Anonymous/Device Login.
[ ] Data Persistence: Save Player Board State (JSON) to Cloud at end of turn.
[ ] Matchmaking: Query Cloud for 7 random opponents near player's MMR.
[x] Lobby System (Simulated):
[x] Replace AIManager bots with "Smart Bots" that scale.
[x] Track HP for the 8-player lobby locally.
[x] MMR System (Rank Floors, Sticky Brackets).

Phase 4: The Content (PvE Modes)

Goal: Roguelite progression where stats can scale wildly.

[ ] Run Manager: Separate persistent "Run" stats vs Base "PvP" stats.
[ ] Dungeon Run (Goal Oriented).
[ ] Endless Mode (Survival).

Phase 5: Monetization (Fair-to-Play)

[ ] Cosmetic Shop (Skins, Boards, Emotes).
[ ] Progression Acceleration.
[ ] Content Packs.

Phase 6: Platform & Meta-Progression (NEW)

Goal: Long-term retention and Steam Launch requirements.

[ ] Player Progression:
[ ] XP System (Earn XP per match based on placement).
[ ] Level-up Rewards (Free currency, basic cosmetics).
[ ] Achievement System:
[ ] Internal Tracker: Track stats like "Triples Created", "Damage Dealt", "Wins with [Hero]".
[ ] UI: "Trophy Case" screen to view unlocked achievements.
[ ] Steamworks Integration:
[ ] Steam Login/Auth.
[ ] Steam Achievements: Sync internal achievements to Steam overlay.
[ ] Steam Cloud Save (Backup for PlayFab).

Future Asset Wishlist (Polish & Juice)

Consider picking these up during sales for high-end polish phases.

[ ] Feel (MMFeedbacks): High-end feedback system for screenshake, chromatic aberration, and haptics.
[ ] Text Animator for Unity: Dynamic text movement for damage numbers and AI coaching.
[ ] Master Audio: Advanced audio pooling, ducking, and variation.