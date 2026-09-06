#include "OdysseyGame.h"
#include "Camera/CameraActor.h"
#include "Camera/CameraComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/StaticMeshActor.h"
#include "Engine/DirectionalLight.h"
#include "Engine/SkyLight.h"
#include "Components/DirectionalLightComponent.h"
#include "Components/SkyLightComponent.h"
#include "Materials/MaterialInstanceDynamic.h"
#include "Kismet/GameplayStatics.h"
#include "Engine/Canvas.h"
#include "Engine/World.h"
#include "InputCoreTypes.h"
#include "UObject/ConstructorHelpers.h"
#include "Engine/StaticMesh.h"

namespace
{
constexpr float Step = 420.f;
const TCHAR* SaveSlot = TEXT("AlbionOdyssey_Local_v1");
FVector PlotPosition(int Cell)
{
    return FVector((Cell % Odyssey::GridSize - 3) * Step, (Cell / Odyssey::GridSize - 3) * Step, 0);
}
const TCHAR* BuildingName(Odyssey::Building B)
{
    switch (B) { case Odyssey::Building::Garden: return TEXT("Garden"); case Odyssey::Building::Library: return TEXT("Library");
    case Odyssey::Building::Observatory: return TEXT("Observatory"); case Odyssey::Building::Hall: return TEXT("Hall"); default: return TEXT("Empty"); }
}
}

AOdysseyGameMode::AOdysseyGameMode()
{
    PlayerControllerClass = AOdysseyController::StaticClass();
    HUDClass = AOdysseyHUD::StaticClass();
    DefaultPawnClass = nullptr;
    // CDO references make primitive fallback meshes discoverable by the cooker.
    static ConstructorHelpers::FObjectFinder<UStaticMesh> Cube(TEXT("/Engine/BasicShapes/Cube.Cube"));
    static ConstructorHelpers::FObjectFinder<UStaticMesh> Sphere(TEXT("/Engine/BasicShapes/Sphere.Sphere"));
    static ConstructorHelpers::FObjectFinder<UStaticMesh> Cylinder(TEXT("/Engine/BasicShapes/Cylinder.Cylinder"));
    static ConstructorHelpers::FObjectFinder<UStaticMesh> Cone(TEXT("/Engine/BasicShapes/Cone.Cone"));
    BuiltinMeshes={Cube.Object,Sphere.Object,Cylinder.Object,Cone.Object};
}

void AOdysseyGameMode::BeginPlay()
{
    Super::BeginPlay();
    if (Load()) Notice = TEXT("Welcome back. Your four local campuses have been restored.");
    auto* Sun = GetWorld()->SpawnActor<ADirectionalLight>(FVector(0,0,3000), FRotator(-55,-30,0));
    if (Sun) Sun->GetLightComponent()->SetIntensity(4.f);
    auto* Sky = GetWorld()->SpawnActor<ASkyLight>();
    if (Sky) { Sky->GetLightComponent()->SetIntensity(1.2f); Sky->GetLightComponent()->SetLightColor(FLinearColor(0.65f,0.76f,1.f)); }
    Rebuild();
}

UStaticMeshComponent* AOdysseyGameMode::Piece(const FString& Shape, FVector Position, FVector Scale, FLinearColor Color, FName Tag)
{
    auto* A = GetWorld()->SpawnActor<AStaticMeshActor>(Position, FRotator::ZeroRotator);
    if (!A) return nullptr;
    Pieces.Add(A);
    if (!Tag.IsNone()) A->Tags.Add(Tag);
    auto* C = A->GetStaticMeshComponent();
    C->SetMobility(EComponentMobility::Movable);
    const FString Path = Shape.StartsWith(TEXT("/")) ? Shape : FString::Printf(TEXT("/Engine/BasicShapes/%s.%s"), *Shape, *Shape);
    C->SetStaticMesh(LoadObject<UStaticMesh>(nullptr, *Path));
    A->SetActorScale3D(Scale);
    if (!Shape.StartsWith(TEXT("/")))
    {
        auto* Base = LoadObject<UMaterialInterface>(nullptr, TEXT("/Game/Generated/M_Odyssey.M_Odyssey"));
        if (Base)
        {
            auto* M = UMaterialInstanceDynamic::Create(Base, A);
            M->SetVectorParameterValue(TEXT("Tint"), Color);
            C->SetMaterial(0, M);
        }
    }
    C->SetCollisionEnabled(ECollisionEnabled::QueryOnly);
    C->SetCollisionResponseToAllChannels(ECR_Block);
    return C;
}

void AOdysseyGameMode::Building(Odyssey::Building Kind, FVector Pos, int Cell)
{
    bool Fantasy = State.Players[State.ActivePlayer].Appearance == Odyssey::Style::Fantasy;
    const FString Name = FString::Printf(TEXT("SM_%s_%s"), BuildingName(Kind), Fantasy ? TEXT("Fantasy") : TEXT("Campus"));
    const FString Path = FString::Printf(TEXT("/Game/Generated/%s.%s"), *Name, *Name);
    const FName Tag(*FString::Printf(TEXT("Plot_%d"), Cell));
    if (LoadObject<UStaticMesh>(nullptr, *Path))
    {
        Piece(Path, Pos, FVector(1), FLinearColor::White, Tag);
        return;
    }
    const FLinearColor Brick = Fantasy ? FLinearColor(0.43,0.23,0.66) : FLinearColor(0.48,0.20,0.12);
    if (Kind == Odyssey::Building::Garden)
    {
        Piece(TEXT("Cylinder"), Pos+FVector(0,0,22), FVector(2.6,2.6,0.4), FLinearColor(0.24,0.5,0.25), Tag);
        Piece(TEXT("Sphere"), Pos+FVector(0,0,145), FVector(1.8), FLinearColor(0.26,0.6,0.30), Tag);
    }
    else
    {
        Piece(TEXT("Cube"), Pos+FVector(0,0,110), FVector(2.8,2.4,2), Brick, Tag);
        Piece(TEXT("Cone"), Pos+FVector(0,0,270), FVector(3.4,3,1.4), FLinearColor(0.13,0.16,0.3), Tag);
        if (Kind == Odyssey::Building::Observatory) Piece(TEXT("Sphere"), Pos+FVector(0,0,300), FVector(1.8), FLinearColor(0.85,0.64,0.22), Tag);
    }
}

void AOdysseyGameMode::Rebuild()
{
    for (auto A : Pieces) if (IsValid(A)) A->Destroy();
    Pieces.Reset();
    const auto& P = State.Players[State.ActivePlayer];
    const bool Fantasy = P.Appearance == Odyssey::Style::Fantasy;
    const FLinearColor Grass = Fantasy ? FLinearColor(0.11,0.22,0.24) : FLinearColor(0.24,0.36,0.16);
    Piece(TEXT("Cube"), FVector(0,0,-130), FVector(36,36,2), FLinearColor(0.07,0.09,0.15));
    for (int Cell=0; Cell<49; ++Cell)
    {
        const FVector Pos=PlotPosition(Cell);
        Piece(TEXT("Cube"), Pos+FVector(0,0,-16), FVector(4.05,4.05,0.3), Grass, FName(*FString::Printf(TEXT("Plot_%d"),Cell)));
        if (P.Plots[Cell] != Odyssey::Building::Empty) Building(P.Plots[Cell],Pos,Cell);
    }
    for (int Id=0; Id<Odyssey::ArtifactCount; ++Id)
    {
        if (P.Memories & (1<<Id)) continue;
        const float Angle=Id*2.f*PI/Odyssey::ArtifactCount;
        const FVector Pos(FMath::Cos(Angle)*2100.f,FMath::Sin(Angle)*2100.f,110);
        const FName Tag(*FString::Printf(TEXT("Memory_%d"),Id));
        Piece(TEXT("Cylinder"), Pos-FVector(0,0,85), FVector(2.6,2.6,0.4), FLinearColor(0.16,0.18,0.3),Tag);
        Piece(TEXT("Sphere"), Pos+FVector(0,0,30), FVector(1.1), FLinearColor(1,0.65,0.12),Tag);
        Piece(TEXT("Cone"), Pos+FVector(0,0,145), FVector(0.6,0.6,0.7), FLinearColor(0.58,0.36,0.91),Tag);
    }
    const float Height=100.f+State.CommunityAcorns*24.f;
    Piece(TEXT("Cylinder"),FVector(0,2900,Height/2),FVector(2.5,2.5,Height/100.f),FLinearColor(0.65,0.45,0.2));
    Piece(TEXT("Sphere"),FVector(0,2900,Height+100),FVector(2),State.CommunityAcorns>=24 ? FLinearColor(0.35,1,0.7) : FLinearColor(0.5,0.35,0.6));
}

void AOdysseyGameMode::Interact(AActor* Target, bool bReclaim)
{
    if (!Target) return;
    for (const FName& Tag : Target->Tags)
    {
        FString S=Tag.ToString();
        if (S.StartsWith(TEXT("Memory_")) && !bReclaim)
        {
            int Id=FCString::Atoi(*S.Mid(7));
            if (Odyssey::Collect(State,State.ActivePlayer,Id)==Odyssey::Result::Ok)
            {
                LastMemory=Id;
                Notice=MemoryTitle(Id)+TEXT(" collected. +3 starlit acorns. Press J to read.");
                Save(); Rebuild();
            }
            return;
        }
        if (S.StartsWith(TEXT("Plot_")))
        {
            if (bExplore) { Notice=TEXT("Press E to return to your builder before changing a plot."); return; }
            const int Cell=FCString::Atoi(*S.Mid(5));
            const auto R=bReclaim ? Odyssey::Reclaim(State,State.ActivePlayer,Cell) : Odyssey::Build(State,State.ActivePlayer,Cell,Selected);
            if (R==Odyssey::Result::Ok) { Notice=bReclaim ? TEXT("Plot reclaimed. All acorns refunded.") : FString(BuildingName(Selected))+TEXT(" built! Your legacy is growing."); Save(); Rebuild(); }
            else if (R==Odyssey::Result::Insufficient) Notice=TEXT("Collect more memories, or reclaim a building for a full refund.");
            else if (R==Odyssey::Result::Occupied) Notice=TEXT("That plot is occupied. Right-click to reclaim it.");
            return;
        }
    }
}

void AOdysseyGameMode::Select(int Number)
{
    if (Number<1 || Number>4) return;
    Selected=static_cast<Odyssey::Building>(Number);
    Notice=FString::Printf(TEXT("%s selected: %d acorns. Click an empty plot."),BuildingName(Selected),Odyssey::Cost(Selected));
}
void AOdysseyGameMode::SwitchPlayer()
{
    State.ActivePlayer=(State.ActivePlayer+1)%Odyssey::PlayerCount;
    LastMemory=-1; bJournal=false;
    Notice=FString::Printf(TEXT("Keeper %d: your own campus, collection, and chosen appearance."),State.ActivePlayer+1);
    Save(); Rebuild();
}
void AOdysseyGameMode::SwitchStyle()
{
    auto& S=State.Players[State.ActivePlayer].Appearance;
    S=S==Odyssey::Style::Fantasy ? Odyssey::Style::Campus : Odyssey::Style::Fantasy;
    Notice=S==Odyssey::Style::Fantasy ? TEXT("The Echo Layer awakens. Welcome to historical fantasy.") : TEXT("Campus appearance: brick, limestone, copper. Concept buildings, not a surveyed replica.");
    Save(); Rebuild();
}
void AOdysseyGameMode::Contribute()
{
    const auto R=Odyssey::Contribute(State,State.ActivePlayer);
    Notice=R==Odyssey::Result::Ok ? TEXT("You added 2 acorns to the shared Constellation Beacon.") : R==Odyssey::Result::Complete ? TEXT("The Constellation Beacon is complete. All Keepers share this legacy!") : TEXT("You need 2 acorns to contribute. Collect a memory or reclaim a plot.");
    if (R==Odyssey::Result::Ok) { if (State.CommunityAcorns==24) Notice=TEXT("Together, you restored the Constellation Beacon!"); Save(); Rebuild(); }
}
FString AOdysseyGameMode::Quest() const
{
    const auto& P=State.Players[State.ActivePlayer];
    if (Odyssey::MemoryCount(P)<3) return FString::Printf(TEXT("FIRST LIGHT / Recover %d more memories"),3-Odyssey::MemoryCount(P));
    if (Odyssey::BuildingCount(P)<3) return FString::Printf(TEXT("LAY ROOTS / Create %d more buildings"),3-Odyssey::BuildingCount(P));
    if (State.CommunityAcorns<24) return TEXT("BETTER TOGETHER / Contribute to the shared Beacon with C");
    return TEXT("LEGACY KEEPER / Beacon complete. Keep exploring and designing.");
}
FString AOdysseyGameMode::MemoryTitle(int Id)
{
    static const TCHAR* Titles[]={TEXT("The First Acorn"),TEXT("Goodrich Echo"),TEXT("The Library Key"),TEXT("Stargazer's Lens"),TEXT("The Painted Leaf"),TEXT("A Table for Everyone"),TEXT("The Science Spark"),TEXT("The Briton Pennant"),TEXT("The Quiet Grove"),TEXT("The Unwritten Page"),TEXT("The Timekeeper"),TEXT("Our Shared Tomorrow")};
    return Id>=0 && Id<12 ? Titles[Id] : TEXT("Memory archive");
}
FString AOdysseyGameMode::MemoryText(int Id)
{
    static const TCHAR* Texts[]={
        TEXT("Fiction: A cosmic squirrel hid a star inside an acorn. Plant it and imagine a campus worth sharing."),
        TEXT("Campus reference: Goodrich Chapel appears on Albion's official campus map. Fiction: its bell guides lost stars home."),
        TEXT("Campus reference: Stockwell Memorial Library is on the official map. Fiction: this key opens a book of possible futures."),
        TEXT("Campus reference: Albion's map lists an Astronomical Observatory. Fiction: its lens reveals the Echo Layer."),
        TEXT("Campus reference: Bobbitt Visual Arts Center is on the official map. Prompt: build a place where every student can create."),
        TEXT("Campus reference: Baldwin Hall is marked for student dining. Prompt: what makes a shared table welcoming?"),
        TEXT("Campus reference: the Science Complex includes the Norris Center. Fiction: a spark escaped an experiment and became a star."),
        TEXT("Fiction: a pennant stitched from constellations celebrates cooperation. Every Keeper can help complete the Beacon."),
        TEXT("Fiction: the grove remembers every kind act. Build a garden where your future self can pause."),
        TEXT("Fiction: an empty page invites your own campus story. Your design does not need to copy anyone else's."),
        TEXT("Fiction: the timekeeper lets you see familiar places through another era. Press T to change the visual layer."),
        TEXT("Fiction: no single Keeper owns the stars. Each local profile can contribute to the same Constellation Beacon.")};
    return Id>=0 && Id<12 ? Texts[Id] : TEXT("Collect a glowing memory around the campus. The latest one opens here. Campus sources: Docs/CampusReferences.md.");
}

void AOdysseyGameMode::Save()
{
    auto* S=Cast<UOdysseySave>(UGameplayStatics::CreateSaveGameObject(UOdysseySave::StaticClass()));
    if (!S) return;
    S->Data.Add(State.ActivePlayer); S->Data.Add(State.CommunityAcorns);
    for (int i=0;i<4;++i)
    {
        const auto& P=State.Players[i];
        S->Data.Add(P.Acorns); S->Data.Add(P.Memories); S->Data.Add(int(P.Appearance)); S->Data.Add(State.Contributions[i]);
        for (auto B:P.Plots) S->Data.Add(int(B));
    }
    if (!UGameplayStatics::SaveGameToSlot(S,SaveSlot,0)) Notice+=TEXT(" Save failed: progress remains in memory for this session.");
}
bool AOdysseyGameMode::Load()
{
    if (!UGameplayStatics::DoesSaveGameExist(SaveSlot,0)) return false;
    auto* S=Cast<UOdysseySave>(UGameplayStatics::LoadGameFromSlot(SaveSlot,0));
    if (!S || S->Version!=1 || S->Data.Num()!=214) return false;
    Odyssey::State Candidate; int At=0;
    Candidate.ActivePlayer=S->Data[At++]; Candidate.CommunityAcorns=S->Data[At++];
    for(int i=0;i<4;++i)
    {
        auto& P=Candidate.Players[i];
        P.Acorns=S->Data[At++];
        const int Memories=S->Data[At++];
        if (Memories<0 || Memories>4095) return false;
        P.Memories=static_cast<uint16>(Memories);
        P.Appearance=static_cast<Odyssey::Style>(S->Data[At++]); Candidate.Contributions[i]=S->Data[At++];
        for(auto& B:P.Plots) B=static_cast<Odyssey::Building>(S->Data[At++]);
    }
    if (!Odyssey::Valid(Candidate)) { Notice=TEXT("Saved data did not pass validation. Starting a fresh session."); return false; }
    State=Candidate; return true;
}

void AOdysseyController::BeginPlay()
{
    Super::BeginPlay();
    bShowMouseCursor=true;
    View=GetWorld()->SpawnActor<ACameraActor>();
    if (View) { View->GetCameraComponent()->FieldOfView=55.f; SetViewTarget(View); }
    SetInputMode(FInputModeGameOnly());
}
void AOdysseyController::PlayerTick(float Dt)
{
    Super::PlayerTick(Dt);
    auto* G=Cast<AOdysseyGameMode>(UGameplayStatics::GetGameMode(this));
    if (!G || !View) return;
    if (WasInputKeyJustPressed(EKeys::One)) G->Select(1);
    if (WasInputKeyJustPressed(EKeys::Two)) G->Select(2);
    if (WasInputKeyJustPressed(EKeys::Three)) G->Select(3);
    if (WasInputKeyJustPressed(EKeys::Four)) G->Select(4);
    if (WasInputKeyJustPressed(EKeys::T)) G->SwitchStyle();
    if (WasInputKeyJustPressed(EKeys::Tab)) G->SwitchPlayer();
    if (WasInputKeyJustPressed(EKeys::C)) G->Contribute();
    if (WasInputKeyJustPressed(EKeys::J)) G->bJournal=!G->bJournal;
    if (G->bJournal && (WasInputKeyJustPressed(EKeys::LeftBracket) || WasInputKeyJustPressed(EKeys::RightBracket)))
    {
        const int Direction=WasInputKeyJustPressed(EKeys::LeftBracket) ? -1 : 1;
        for (int i=0;i<12;++i)
        {
            G->LastMemory=(G->LastMemory+Direction+12)%12;
            if (G->State.Players[G->State.ActivePlayer].Memories & (1<<G->LastMemory)) break;
        }
        if (Odyssey::MemoryCount(G->State.Players[G->State.ActivePlayer])==0) G->LastMemory=-1;
    }
    if (WasInputKeyJustPressed(EKeys::E)) { G->bExplore=!G->bExplore; G->Notice=G->bExplore ? TEXT("Room exploration: WASD to travel, Q/R to orbit. No GPS or camera required.") : TEXT("Campus builder: choose 1-4, then click a plot."); }
    if (WasInputKeyJustPressed(EKeys::Home)) { Focus=FVector::ZeroVector; Distance=5000; Yaw=-45; }
    if (WasInputKeyJustPressed(EKeys::Q)) Yaw-=15;
    if (WasInputKeyJustPressed(EKeys::R)) Yaw+=15;
    if (WasInputKeyJustPressed(EKeys::MouseScrollUp)) Distance=FMath::Max(1200.f,Distance-300.f);
    if (WasInputKeyJustPressed(EKeys::MouseScrollDown)) Distance=FMath::Min(8000.f,Distance+300.f);
    const FRotator Orbit(0,Yaw,0);
    const FVector Forward=Orbit.Vector(), Right=FRotationMatrix(Orbit).GetUnitAxis(EAxis::Y);
    if (IsInputKeyDown(EKeys::W)) Focus+=Forward*Dt*1100;
    if (IsInputKeyDown(EKeys::S)) Focus-=Forward*Dt*1100;
    if (IsInputKeyDown(EKeys::D)) Focus+=Right*Dt*1100;
    if (IsInputKeyDown(EKeys::A)) Focus-=Right*Dt*1100;
    Focus.X=FMath::Clamp(Focus.X,-3000.f,3000.f); Focus.Y=FMath::Clamp(Focus.Y,-3000.f,3500.f);
    const FVector Offset=FRotator(-52,Yaw,0).Vector()*-Distance;
    View->SetActorLocation(Focus+Offset);
    View->SetActorRotation((Focus-View->GetActorLocation()).Rotation());
    if (!G->bJournal && (WasInputKeyJustPressed(EKeys::LeftMouseButton) || WasInputKeyJustPressed(EKeys::RightMouseButton)))
    {
        int Width=0,Height=0; GetViewportSize(Width,Height);
        float MouseX=0,MouseY=0;
        const float Scale=FMath::Max(0.6f,FMath::Min(Width/1280.f,Height/800.f));
        if (!GetMousePosition(MouseX,MouseY) || MouseY<140*Scale || MouseY>Height-154*Scale) return;
        FHitResult Hit;
        if (GetHitResultUnderCursor(ECC_Visibility,false,Hit)) G->Interact(Hit.GetActor(),WasInputKeyJustPressed(EKeys::RightMouseButton));
    }
}

void AOdysseyHUD::DrawHUD()
{
    Super::DrawHUD();
    auto* G=Cast<AOdysseyGameMode>(UGameplayStatics::GetGameMode(this));
    if (!G || !Canvas) return;
    const float S=FMath::Max(0.6f,FMath::Min(Canvas->SizeX/1280.f,Canvas->SizeY/800.f));
    const float W=Canvas->SizeX, H=Canvas->SizeY;
    const FLinearColor Ink(0.025,0.035,0.07,0.94), Gold(1,0.76,0.35), White(0.92,0.94,1), Muted(0.6,0.7,0.8);
    const auto Text=[&](const FString& V,float X,float Y,float Size,FLinearColor Color){ DrawText(V,Color,X*S,Y*S,nullptr,Size*S,false); };
    const auto& P=G->State.Players[G->State.ActivePlayer];
    DrawRect(Ink,0,0,W,140*S);
    Text(TEXT("COSMIC SQUIRRELS"),28,18,1.1,Gold);
    Text(TEXT("ALBION ODYSSEY"),28,42,2.5,White);
    Text(FString::Printf(TEXT("KEEPER %d  /  %s  /  ROOM PLAY"),G->State.ActivePlayer+1,P.Appearance==Odyssey::Style::Fantasy ? TEXT("ECHO FANTASY") : TEXT("CAMPUS CONCEPT")),28,88,1.1,Muted);
    Text(FString::Printf(TEXT("%d ACORNS     %d / 12 MEMORIES     %d / 24 BEACON"),P.Acorns,Odyssey::MemoryCount(P),G->State.CommunityAcorns),600,30,1.2,Gold);
    Text(G->Quest(),600,62,1.1,White);
    Text(TEXT("Local pass-and-play / TAB changes Keeper"),600,92,1,Muted);
    DrawRect(Ink,0,H-154*S,W,154*S);
    const float Bottom=H/S-140;
    Text(G->Notice,28,Bottom,1.05,Gold);
    Text(FString::Printf(TEXT("BUILD  1 Garden (2)   2 Library (4)   3 Observatory (6)   4 Hall (3)   |   Selected: %s"),BuildingName(G->Selected)),28,Bottom+30,1.05,White);
    Text(TEXT("CLICK collect / build    RIGHT-CLICK reclaim    C contribute    T change style    J memory journal"),28,Bottom+58,1,White);
    Text(TEXT("WASD move    Q / R orbit    SCROLL zoom    HOME reset view    E explore / build    TAB next Keeper"),28,Bottom+85,1,Muted);
    Text(TEXT("EARLY PROTOTYPE / Original concept models. Campus facts and fictional stories are labeled separately."),28,Bottom+112,0.85,Muted);
    if (G->bJournal)
    {
        DrawRect(Ink,80*S,170*S,W-160*S,H-350*S);
        Text(TEXT("THE MEMORY ARCHIVE"),110,195,1.8,Gold);
        Text(G->MemoryTitle(G->LastMemory),110,239,1.4,White);
        // Short wrapped rows keep the journal readable without a UMG asset dependency.
        TArray<FString> Words; G->MemoryText(G->LastMemory).ParseIntoArray(Words,TEXT(" "),true);
        FString Line; float Y=280;
        const int Limit=FMath::Max(35,int((W/S-230)/9));
        for (const auto& Word:Words)
        {
            if (Line.Len()+Word.Len()>Limit) { Text(Line,110,Y,1.1,White); Y+=26; Line.Empty(); }
            Line+=Word+TEXT(" ");
        }
        Text(Line,110,Y,1.1,White);
        Text(TEXT("[ / ] browse collected memories. J returns to your campus."),110,Y+50,1,Muted);
    }
}
