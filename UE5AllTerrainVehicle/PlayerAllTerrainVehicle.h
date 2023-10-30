// Sample C++ code from UE5 vehicle project by Ian Snyder

#pragma once

#include "CoreMinimal.h"
#include "AllTerrainVehicle.h"
#include "GameFramework/SpringArmComponent.h"
#include "Camera/CameraComponent.h"
#include "PlayerAllTerrainVehicle.generated.h"

/**
 * 
 */
UCLASS()
class APlayerAllTerrainVehicle : public AAllTerrainVehicle
{
	GENERATED_BODY()
	public:

	// Sets default values for this pawn's properties.
	APlayerAllTerrainVehicle();

	// Spring arm for positioning the camera above the ATV
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = AllTerrainVehicle)
		USpringArmComponent* SpringArm = nullptr;

	// Camera to view the ATV
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = AllTerrainVehicle)
		UCameraComponent* Camera = nullptr;

	// Force for general movement
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = AllTerrainVehicle)
		float ControllerForce = 250.0f;

	// Jump Force
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = AllTerrainVehicle)
		float JumpForce = 50.0f;

	// Dash Force
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = AllTerrainVehicle)
		float DashForce = 150.0f;

	// How long dashing should last
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = AllTerrainVehicle)
		float DashTime = 1.5f;

	// The maximum speed in meters per second
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = AllTerrainVehicle)
		float MaximumSpeed = 4.0f;

	// Max turn speed
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category= AllTerrainVehicle)
		float MaximumTurnSpeed = 10.0f;

	// DEBUG Draw debug lines for movement vectors
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category=AllTerrainVehicle)
		bool DrawDebugVectorLines = false;
	// DEBUG length of test line renderer
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category=  AllTerrainVehicle)
		float DebugLineLength = 100.0f;

	// DEBUG How long to show the debug lines
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category=  AllTerrainVehicle)
		float DebugLineTime = 0.3f;

	// DEBUG Thickness of debug lines
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category=  AllTerrainVehicle)
		float DebugLineThickness = 10.0f;

	// DEBUG Debug Line color
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category=  AllTerrainVehicle)
		FColor DebugLineColor = FColor::Red;

protected:

	// Control the movement of All Terrain Vehicle, called every frame.
	virtual void Tick(float DeltaSeconds) override;

	// Called to bind functionality to input.
	virtual void SetupPlayerInputComponent(class UInputComponent* PlayerInputComponent) override;

private:

	// Move the All Terrain Vehicle forward on the X axis
	void MoveForward(float value)
	{
		InputForward = value;
	}

	// Turn the All Terrain Vehicle
	void Turn(float value)
	{
		InputTurn = value;
	}

	// Jump!
	void Jump();

	// Dash!
	void Dash();

	// The current forward input received from the player.
	float InputForward = 0.0f;

	// The current turning input received from the player.
	float InputTurn = 0.0f;
	
	// Timer used to control how long dashing lasts
	float TempDashTimer = 0.0f;

	// Allow the HUD unfettered access to this class.
	friend class AAllTerrainVehicleHUD;
};
