#include "OdysseyPersistence.h"
#include "OdysseyGame.h"
#include "Kismet/GameplayStatics.h"

namespace OdysseyPersistence
{
static const TCHAR* Slot = TEXT("AlbionOdyssey_Local_v1");
bool Save(const Odyssey::State& State)
{
    if (!Odyssey::Valid(State)) return false;
    auto* Saved=Cast<UOdysseySave>(UGameplayStatics::CreateSaveGameObject(UOdysseySave::StaticClass()));
    if (!Saved) return false;
    Saved->Data.Add(State.ActivePlayer); Saved->Data.Add(State.CommunityAcorns);
    for (int i=0;i<4;++i)
    {
        const auto& P=State.Players[i];
        Saved->Data.Add(P.Acorns); Saved->Data.Add(P.Memories); Saved->Data.Add(int(P.Appearance)); Saved->Data.Add(State.Contributions[i]);
        for (auto B:P.Plots) Saved->Data.Add(int(B));
    }
    return UGameplayStatics::SaveGameToSlot(Saved,Slot,0);
}
bool Load(Odyssey::State& State)
{
    if (!UGameplayStatics::DoesSaveGameExist(Slot,0)) return false;
    auto* Saved=Cast<UOdysseySave>(UGameplayStatics::LoadGameFromSlot(Slot,0));
    if (!Saved || Saved->Version!=1 || Saved->Data.Num()!=214) return false;
    Odyssey::State Candidate; int At=0;
    Candidate.ActivePlayer=Saved->Data[At++]; Candidate.CommunityAcorns=Saved->Data[At++];
    for(int i=0;i<4;++i)
    {
        auto& P=Candidate.Players[i]; P.Acorns=Saved->Data[At++];
        const int Memories=Saved->Data[At++];
        if (Memories<0 || Memories>4095) return false;
        P.Memories=static_cast<uint16>(Memories);
        P.Appearance=static_cast<Odyssey::Style>(Saved->Data[At++]); Candidate.Contributions[i]=Saved->Data[At++];
        for(auto& B:P.Plots) B=static_cast<Odyssey::Building>(Saved->Data[At++]);
    }
    if (!Odyssey::Valid(Candidate)) return false;
    State=Candidate; return true;
}
}
