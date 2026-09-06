#pragma once
#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "GameFramework/PlayerController.h"
#include "GameFramework/HUD.h"
#include "GameFramework/SaveGame.h"
#include "Core/OdysseyRules.h"
#include "OdysseyGame.generated.h"

class UStaticMeshComponent;
class UStaticMesh;
class ACameraActor;

UCLASS()
class ALBIONODYSSEY_API UOdysseySave : public USaveGame
{
    GENERATED_BODY()
public:
    UPROPERTY(SaveGame) int32 Version = 1;
    UPROPERTY(SaveGame) TArray<int32> Data;
};

UCLASS()
class ALBIONODYSSEY_API AOdysseyGameMode : public AGameModeBase
{
    GENERATED_BODY()
public:
    AOdysseyGameMode();
    virtual void BeginPlay() override;
    Odyssey::State State;
    Odyssey::Building Selected = Odyssey::Building::Garden;
    bool bExplore = false;
    bool bJournal = false;
    FString Notice = TEXT("Welcome, Keeper. Collect a glowing memory, then build your legacy.");
    int LastMemory = -1;
    void Interact(AActor* Target, bool bReclaim = false);
    void SwitchPlayer();
    void SwitchStyle();
    void Contribute();
    void Rebuild();
    void Save();
    bool Load();
    void Select(int Number);
    FString Quest() const;
    static FString MemoryTitle(int Id);
    static FString MemoryText(int Id);
private:
    UPROPERTY() TArray<TObjectPtr<AActor>> Pieces;
    UPROPERTY() TArray<TObjectPtr<UStaticMesh>> BuiltinMeshes;
    UStaticMeshComponent* Piece(const FString& Shape, FVector Position, FVector Scale, FLinearColor Color, FName Tag = NAME_None);
    void Building(Odyssey::Building Kind, FVector Position, int Cell);
};

UCLASS()
class ALBIONODYSSEY_API AOdysseyController : public APlayerController
{
    GENERATED_BODY()
public:
    virtual void BeginPlay() override;
    virtual void PlayerTick(float DeltaTime) override;
private:
    UPROPERTY() TObjectPtr<ACameraActor> View;
    FVector Focus = FVector::ZeroVector;
    float Yaw = -45.f;
    float Distance = 5000.f;
};

UCLASS()
class ALBIONODYSSEY_API AOdysseyHUD : public AHUD
{
    GENERATED_BODY()
public:
    virtual void DrawHUD() override;
};
