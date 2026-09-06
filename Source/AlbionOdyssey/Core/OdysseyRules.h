#pragma once
#include <array>
#include <algorithm>
#include <cstdint>

// Engine-independent rules: one source of truth for the game and fast CI tests.
namespace Odyssey
{
constexpr int PlayerCount = 4;
constexpr int GridSize = 7;
constexpr int ArtifactCount = 12;
constexpr int CommunityGoal = 24;
enum class Style : int { Campus, Fantasy };
enum class Building : int { Empty, Garden, Library, Observatory, Hall };
enum class Result { Ok, Invalid, Occupied, Insufficient, AlreadyCollected, Complete };
constexpr int Cost(Building b)
{
    return b == Building::Garden ? 2 : b == Building::Library ? 4 :
           b == Building::Observatory ? 6 : b == Building::Hall ? 3 : 0;
}
struct Profile
{
    int Acorns = 6;
    std::uint16_t Memories = 0;
    Style Appearance = Style::Fantasy;
    std::array<Building, GridSize * GridSize> Plots{};
};
struct State
{
    std::array<Profile, PlayerCount> Players{};
    int ActivePlayer = 0;
    int CommunityAcorns = 0;
    std::array<int, PlayerCount> Contributions{};
};
inline bool ValidPlayer(int p) { return p >= 0 && p < PlayerCount; }
inline bool ValidBuilding(Building b) { return int(b) >= 1 && int(b) <= 4; }
inline int MemoryCount(const Profile& p)
{
    int n = 0;
    for (int i = 0; i < ArtifactCount; ++i) n += (p.Memories >> i) & 1;
    return n;
}
inline int BuildingCount(const Profile& p)
{
    return int(std::count_if(p.Plots.begin(), p.Plots.end(), [](Building b) { return b != Building::Empty; }));
}
inline Result Collect(State& s, int player, int artifact)
{
    if (!ValidPlayer(player) || artifact < 0 || artifact >= ArtifactCount) return Result::Invalid;
    auto& p = s.Players[player];
    const auto bit = std::uint16_t(1 << artifact);
    if (p.Memories & bit) return Result::AlreadyCollected;
    p.Memories |= bit;
    p.Acorns += 3;
    return Result::Ok;
}
inline Result Build(State& s, int player, int cell, Building b)
{
    if (!ValidPlayer(player) || cell < 0 || cell >= GridSize * GridSize || !ValidBuilding(b)) return Result::Invalid;
    auto& p = s.Players[player];
    if (p.Plots[cell] != Building::Empty) return Result::Occupied;
    if (p.Acorns < Cost(b)) return Result::Insufficient;
    p.Acorns -= Cost(b);
    p.Plots[cell] = b;
    return Result::Ok;
}
inline Result Reclaim(State& s, int player, int cell)
{
    if (!ValidPlayer(player) || cell < 0 || cell >= GridSize * GridSize) return Result::Invalid;
    auto& p = s.Players[player];
    if (!ValidBuilding(p.Plots[cell])) return Result::Invalid;
    p.Acorns += Cost(p.Plots[cell]); // Full refund encourages experimentation, no resource trap.
    p.Plots[cell] = Building::Empty;
    return Result::Ok;
}
inline Result Contribute(State& s, int player)
{
    if (!ValidPlayer(player)) return Result::Invalid;
    if (s.CommunityAcorns >= CommunityGoal) return Result::Complete;
    if (s.Players[player].Acorns < 2) return Result::Insufficient;
    s.Players[player].Acorns -= 2;
    s.Contributions[player] += 2;
    s.CommunityAcorns += 2;
    return Result::Ok;
}
inline bool Valid(const State& s)
{
    if (!ValidPlayer(s.ActivePlayer) || s.CommunityAcorns < 0 || s.CommunityAcorns > CommunityGoal) return false;
    int total = 0;
    for (int i = 0; i < PlayerCount; ++i)
    {
        const auto& p = s.Players[i];
        if (p.Acorns < 0 || p.Acorns > 42 || p.Memories >= (1 << ArtifactCount)) return false;
        if (p.Appearance != Style::Campus && p.Appearance != Style::Fantasy) return false;
        if (s.Contributions[i] < 0 || s.Contributions[i] > CommunityGoal || s.Contributions[i] % 2) return false;
        int spent = s.Contributions[i];
        for (auto b : p.Plots)
        {
            if (b != Building::Empty && !ValidBuilding(b)) return false;
            spent += Cost(b);
        }
        if (p.Acorns + spent != 6 + 3 * MemoryCount(p)) return false;
        total += s.Contributions[i];
    }
    return total == s.CommunityAcorns;
}
}
