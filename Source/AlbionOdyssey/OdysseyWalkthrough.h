#pragma once
#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "GameFramework/GameModeBase.h"
#include "GameFramework/PlayerController.h"
#include "GameFramework/HUD.h"
#include "Core/OdysseyRules.h"
#include "OdysseyWalkthrough.generated.h"

class UCameraComponent;
class UStaticMeshComponent;
class UStaticMesh;

UCLASS()
class ALBIONODYSSEY_API AOdysseyExplorer : public ACharacter
{
    GENERATED_BODY()
public:
    AOdysseyExplorer();
    virtual void SetupPlayerInputComponent(UInputComponent* Input) override;
    UPROPERTY(VisibleAnywhere) TObjectPtr<UCameraComponent> Eyes;
private:
    void Forward(float Amount);
    void Right(float Amount);
    void LookX(float Amount);
    void LookY(float Amount);
    void SprintStart();
    void SprintEnd();
    void Interact();
};

UCLASS()
class ALBIONODYSSEY_API AOdysseyWalkController : public APlayerController
{
    GENERATED_BODY()
public:
    virtual void BeginPlay() override;
    virtual void PlayerTick(float DeltaSeconds) override;
};

UCLASS()
class ALBIONODYSSEY_API AOdysseyWalkMode : public AGameModeBase
{
    GENERATED_BODY()
public:
    AOdysseyWalkMode();
    virtual void BeginPlay() override;
    Odyssey::State State;
    FString Notice = TEXT("Walk through the entrance. Find a memory on each floor.");
    void Collect(AActor* Target);
    void RefreshMemories();
    void SwitchPlayer();
private:
    UPROPERTY() TArray<TObjectPtr<AActor>> Memories;
    UPROPERTY() TObjectPtr<UStaticMesh> MemoryMesh;
};

UCLASS()
class ALBIONODYSSEY_API AOdysseyWalkHUD : public AHUD
{
    GENERATED_BODY()
public:
    virtual void DrawHUD() override;
};
