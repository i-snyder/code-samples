// Sample C++ code from UE5 vehicle project by Ian Snyder

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Pawn.h"
#include "AllTerrainVehicle.generated.h"

UCLASS()
class AAllTerrainVehicle : public APawn
{
	GENERATED_BODY()

public:
	// Sets default values for this pawn's properties
	AAllTerrainVehicle();

	// The skeletal mesh that represents the All Terrain Vehicle.
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = AllTerrainVehicle)
	USkeletalMeshComponent* ATVMesh = nullptr;

	// Reset the location to its initial location when spawned.
	UFUNCTION(BlueprintCallable, Category="All Terrain Vehicle")
	void ResetLocation() const
	{
		ATVMesh->SetWorldLocation(InitialLocation + FVector(0.0f, 0.0f, 150.0f));
		ATVMesh->SetPhysicsLinearVelocity(FVector::ZeroVector);
		ATVMesh->SetPhysicsAngularVelocityInDegrees(FVector::ZeroVector);
	}

protected:
	// Called when the game starts or when spawned
	virtual void BeginPlay() override;

public:	
	// Called every frame
	virtual void Tick(float DeltaTime) override;

	// Called to bind functionality to input
	virtual void SetupPlayerInputComponent(class UInputComponent* PlayerInputComponent) override;

	// Receive notification of a collision contact and record that we're in contact with something.
	virtual void NotifyHit(UPrimitiveComponent* MyComp, AActor* Other, UPrimitiveComponent* OtherComp, const bool bSelfMoved, const FVector HitLocation, const FVector HitNormal, const FVector NormalImpulse, const FHitResult& Hit) override
	{
		Super::NotifyHit(MyComp, Other, OtherComp, bSelfMoved, HitLocation, HitNormal, NormalImpulse, Hit);

		InContact = true;
	}

	bool InContact = false;

private:
	// The initial location of the All Terrain Vehicle at game start.
	FVector InitialLocation = FVector::ZeroVector;
};
