#include "OdysseyWalkthrough.h"
#include "OdysseyPersistence.h"
#include "OdysseyGame.h"
#include "Camera/CameraComponent.h"
#include "Components/CapsuleComponent.h"
#include "Components/InputComponent.h"
#include "Components/StaticMeshComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Kismet/GameplayStatics.h"
#include "Engine/StaticMeshActor.h"
#include "Engine/StaticMesh.h"
#include "Engine/World.h"
#include "Engine/Canvas.h"
#include "UObject/ConstructorHelpers.h"
#include "InputCoreTypes.h"
#include "Materials/MaterialInterface.h"

AOdysseyExplorer::AOdysseyExplorer()
{
    GetCapsuleComponent()->InitCapsuleSize(42.f,96.f);
    Eyes=CreateDefaultSubobject<UCameraComponent>(TEXT("PlayerEyes"));
    Eyes->SetupAttachment(GetCapsuleComponent());
    Eyes->SetRelativeLocation(FVector(0,0,69.f)); // Eye height: 1.65 m above the floor.
    Eyes->bUsePawnControlRotation=true;
    Eyes->FieldOfView=90.f;
    bUseControllerRotationYaw=true;
    GetCharacterMovement()->MaxWalkSpeed=380.f;
    GetCharacterMovement()->MaxStepHeight=42.f;
    GetCharacterMovement()->JumpZVelocity=440.f;
    GetCharacterMovement()->AirControl=.25f;
}
void AOdysseyExplorer::SetupPlayerInputComponent(UInputComponent* Input)
{
    Super::SetupPlayerInputComponent(Input);
    Input->BindAxis(TEXT("WalkForward"),this,&AOdysseyExplorer::Forward);
    Input->BindAxis(TEXT("WalkRight"),this,&AOdysseyExplorer::Right);
    Input->BindAxis(TEXT("LookHorizontal"),this,&AOdysseyExplorer::LookX);
    Input->BindAxis(TEXT("LookVertical"),this,&AOdysseyExplorer::LookY);
    Input->BindAction(TEXT("Jump"),IE_Pressed,this,&ACharacter::Jump);
    Input->BindAction(TEXT("Jump"),IE_Released,this,&ACharacter::StopJumping);
    Input->BindAction(TEXT("Sprint"),IE_Pressed,this,&AOdysseyExplorer::SprintStart);
    Input->BindAction(TEXT("Sprint"),IE_Released,this,&AOdysseyExplorer::SprintEnd);
    Input->BindAction(TEXT("Collect"),IE_Pressed,this,&AOdysseyExplorer::Interact);
}
void AOdysseyExplorer::Forward(float Amount)
{
    if (Controller) AddMovementInput(FRotator(0,Controller->GetControlRotation().Yaw,0).Vector(),Amount);
}
void AOdysseyExplorer::Right(float Amount)
{
    if (Controller) AddMovementInput(FRotationMatrix(FRotator(0,Controller->GetControlRotation().Yaw,0)).GetUnitAxis(EAxis::Y),Amount);
}
void AOdysseyExplorer::LookX(float Amount) { AddControllerYawInput(Amount); }
void AOdysseyExplorer::LookY(float Amount) { AddControllerPitchInput(Amount); }
void AOdysseyExplorer::SprintStart() { GetCharacterMovement()->MaxWalkSpeed=650.f; }
void AOdysseyExplorer::SprintEnd() { GetCharacterMovement()->MaxWalkSpeed=380.f; }
void AOdysseyExplorer::Interact()
{
    auto* Game=Cast<AOdysseyWalkMode>(UGameplayStatics::GetGameMode(this));
    if (!Game) return;
    FHitResult Hit;
    FCollisionQueryParams Query(SCENE_QUERY_STAT(CollectMemory),false,this);
    const FVector Start=Eyes->GetComponentLocation();
    if (GetWorld()->LineTraceSingleByChannel(Hit,Start,Start+Eyes->GetForwardVector()*350.f,ECC_Visibility,Query)) Game->Collect(Hit.GetActor());
}
void AOdysseyWalkController::BeginPlay()
{
    Super::BeginPlay();
    bShowMouseCursor=false;
    SetInputMode(FInputModeGameOnly());
}
void AOdysseyWalkController::PlayerTick(float Dt)
{
    Super::PlayerTick(Dt);
    auto* G=Cast<AOdysseyWalkMode>(UGameplayStatics::GetGameMode(this));
    if (!G) return;
    if (WasInputKeyJustPressed(EKeys::F2))
    {
        if (OdysseyPersistence::Save(G->State)) UGameplayStatics::OpenLevel(this,FName(TEXT("LegacyCampus")));
        else G->Notice=TEXT("Save failed. Staying here to preserve your current progress.");
        return;
    }
    if (WasInputKeyJustPressed(EKeys::Tab)) G->SwitchPlayer();
}

AOdysseyWalkMode::AOdysseyWalkMode()
{
    DefaultPawnClass=AOdysseyExplorer::StaticClass();
    PlayerControllerClass=AOdysseyWalkController::StaticClass();
    HUDClass=AOdysseyWalkHUD::StaticClass();
    static ConstructorHelpers::FObjectFinder<UStaticMesh> Sphere(TEXT("/Engine/BasicShapes/Sphere.Sphere"));
    MemoryMesh=Sphere.Object;
}
void AOdysseyWalkMode::BeginPlay()
{
    Super::BeginPlay();
    OdysseyPersistence::Load(State);
    RefreshMemories();
}
void AOdysseyWalkMode::RefreshMemories()
{
    for (auto A:Memories) if (IsValid(A)) A->Destroy();
    Memories.Reset();
    for (int i=0;i<8;++i)
    {
        if (State.Players[State.ActivePlayer].Memories & (1<<i)) continue;
        auto* A=GetWorld()->SpawnActor<AStaticMeshActor>(FVector(0,300,i*360+115),FRotator::ZeroRotator);
        if (!A) continue;
        A->Tags.Add(FName(*FString::Printf(TEXT("TowerMemory_%d"),i)));
        auto* Mesh=A->GetStaticMeshComponent();
        Mesh->SetMobility(EComponentMobility::Movable);
        Mesh->SetStaticMesh(MemoryMesh);
        Mesh->SetWorldScale3D(FVector(.45f));
        if (auto* Material=LoadObject<UMaterialInterface>(nullptr,TEXT("/Game/Architecture/M_Architecture_Memory.M_Architecture_Memory"))) Mesh->SetMaterial(0,Material);
        Mesh->SetCollisionEnabled(ECollisionEnabled::QueryOnly);
        Mesh->SetCollisionResponseToAllChannels(ECR_Ignore);
        Mesh->SetCollisionResponseToChannel(ECC_Visibility,ECR_Block);
        Memories.Add(A);
    }
}
void AOdysseyWalkMode::Collect(AActor* Target)
{
    if (!Target) return;
    for (const auto& Tag:Target->Tags)
    {
        const FString Name=Tag.ToString();
        if (!Name.StartsWith(TEXT("TowerMemory_"))) continue;
        int Id=FCString::Atoi(*Name.Mid(12));
        if (Odyssey::Collect(State,State.ActivePlayer,Id)==Odyssey::Result::Ok)
        {
            Notice=AOdysseyGameMode::MemoryTitle(Id)+TEXT(" recovered. +3 acorns for your campus. F2 opens the builder.");
            if (!OdysseyPersistence::Save(State)) Notice+=TEXT(" Save failed; progress is held in memory.");
            RefreshMemories();
        }
        return;
    }
}
void AOdysseyWalkMode::SwitchPlayer()
{
    State.ActivePlayer=(State.ActivePlayer+1)%Odyssey::PlayerCount;
    Notice=FString::Printf(TEXT("Keeper %d. Walk, collect, then build your own campus with F2."),State.ActivePlayer+1);
    if (!OdysseyPersistence::Save(State)) Notice+=TEXT(" Save failed.");
    RefreshMemories();
}
void AOdysseyWalkHUD::DrawHUD()
{
    Super::DrawHUD();
    auto* G=Cast<AOdysseyWalkMode>(UGameplayStatics::GetGameMode(this));
    if (!G || !Canvas) return;
    const auto* Pawn=GetOwningPawn();
    const int Floor=Pawn ? FMath::Clamp(FMath::FloorToInt((Pawn->GetActorLocation().Z-91.f)/360.f)+1,1,8) : 1;
    const auto& P=G->State.Players[G->State.ActivePlayer];
    const float S=FMath::Max(.65f,FMath::Min(Canvas->SizeX/1280.f,Canvas->SizeY/800.f));
    const FLinearColor Ink(.018,.025,.04,.87),White(.95,.95,.92),Gold(1,.75,.25);
    DrawRect(Ink,20*S,20*S,560*S,120*S);
    DrawText(TEXT("ALBION ODYSSEY  /  LEGACY HALL"),Gold,38*S,32*S,nullptr,1.45f*S);
    DrawText(FString::Printf(TEXT("FLOOR %d / 8     KEEPER %d     %d ACORNS"),Floor,G->State.ActivePlayer+1,P.Acorns),White,38*S,66*S,nullptr,1.15f*S);
    DrawText(TEXT("Full-size architecture / Original 32.8 m campus tower"),White,38*S,98*S,nullptr,.95f*S);
    DrawRect(Ink,0,Canvas->SizeY-90*S,Canvas->SizeX,90*S);
    DrawText(G->Notice,Gold,24*S,Canvas->SizeY-75*S,nullptr,1.0f*S);
    DrawText(TEXT("WASD walk  /  MOUSE look  /  SHIFT run  /  SPACE jump  /  E collect  /  TAB Keeper  /  F2 builder"),White,24*S,Canvas->SizeY-42*S,nullptr,.95f*S);
    const float CX=Canvas->SizeX/2.f,CY=Canvas->SizeY/2.f;
    DrawLine(CX-5,CY,CX+5,CY,White,1.f); DrawLine(CX,CY-5,CX,CY+5,White,1.f);
}
