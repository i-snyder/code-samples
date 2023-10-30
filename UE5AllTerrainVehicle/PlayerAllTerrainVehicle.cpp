// Sample C++ code from UE5 vehicle project by Ian Snyder

#include "PlayerAllTerrainVehicle.h"
#include "GameFramework/PlayerInput.h"
#include "Components/InputComponent.h"

APlayerAllTerrainVehicle::APlayerAllTerrainVehicle()
{
	// Create a spring-arm attached to the ATV mesh.
	SpringArm = CreateDefaultSubobject<USpringArmComponent>(TEXT("SpringArm"));
	SpringArm->bDoCollisionTest = false;
	SpringArm->SetUsingAbsoluteRotation(false);
	SpringArm->SetRelativeRotation(FRotator(-45.0f, 0.0f, 0.0f));
	SpringArm->TargetArmLength = 1000.0f;
	SpringArm->bEnableCameraLag = false;
	SpringArm->CameraLagSpeed = 5.0f;
	SpringArm->SetupAttachment(ATVMesh);

	// Create a camera and attach to the spring-arm.
	Camera = CreateDefaultSubobject<UCameraComponent>(TEXT("Camera"));
	Camera->bUsePawnControlRotation = false;
	Camera->SetupAttachment(SpringArm, USpringArmComponent::SocketName);
}

// Init default pawn input bindings
static void InitializeDefaultPawnInputBindings()
{
	static bool bindingsAdded = false;

	if (bindingsAdded == false)
	{
		bindingsAdded = true;
		UPlayerInput::AddEngineDefinedAxisMapping(FInputAxisKeyMapping("AllTerrainVehicle_MoveForward", EKeys::W, 1.f));
		UPlayerInput::AddEngineDefinedAxisMapping(FInputAxisKeyMapping("AllTerrainVehicle_MoveForward", EKeys::S, -1.f));
		UPlayerInput::AddEngineDefinedAxisMapping(FInputAxisKeyMapping("AllTerrainVehicle_MoveForward", EKeys::Up, 1.f));
		UPlayerInput::AddEngineDefinedAxisMapping(FInputAxisKeyMapping("AllTerrainVehicle_MoveForward", EKeys::Down, -1.f));
		UPlayerInput::AddEngineDefinedAxisMapping(FInputAxisKeyMapping("AllTerrainVehicle_MoveForward", EKeys::Gamepad_LeftY, 1.f));

		UPlayerInput::AddEngineDefinedAxisMapping(FInputAxisKeyMapping("AllTerrainVehicle_Turn", EKeys::A, -1.f));
		UPlayerInput::AddEngineDefinedAxisMapping(FInputAxisKeyMapping("AllTerrainVehicle_Turn", EKeys::D, 1.f));
		UPlayerInput::AddEngineDefinedAxisMapping(FInputAxisKeyMapping("AllTerrainVehicle_Turn", EKeys::Left, -1.f));
		UPlayerInput::AddEngineDefinedAxisMapping(FInputAxisKeyMapping("AllTerrainVehicle_Turn", EKeys::Right, 1.f));
		UPlayerInput::AddEngineDefinedAxisMapping(FInputAxisKeyMapping("AllTerrainVehicle_Turn", EKeys::Gamepad_LeftX, 1.f));

		UPlayerInput::AddEngineDefinedActionMapping(FInputActionKeyMapping("AllTerrainVehicle_Jump", EKeys::Enter));
		UPlayerInput::AddEngineDefinedActionMapping(FInputActionKeyMapping("AllTerrainVehicle_Dash", EKeys::SpaceBar));
	}
}

void APlayerAllTerrainVehicle::Tick(const float DeltaSeconds)
{
	Super::Tick(DeltaSeconds);

	// Speed handling
	FVector Velocity = ATVMesh->GetPhysicsLinearVelocity();
	const float z = Velocity.Z; // Store the z value so we can modify the X and Y e-Z-ly ;)

	Velocity.Z = 0.0f;

	if (Velocity.Size() > MaximumSpeed * 100.0f)
	{
		Velocity.Normalize();
		Velocity *= MaximumSpeed * 100.0f;
		Velocity.Z = z;

		const float BrakingRatio = FMath::Pow(1.0f - FMath::Min(TempDashTimer, 1.0f), 2.0f);

		const FVector MergedVelocity = FMath::Lerp(ATVMesh->GetPhysicsLinearVelocity(), Velocity, BrakingRatio);

		ATVMesh->SetPhysicsLinearVelocity(MergedVelocity);
	} else
	{
		ATVMesh->AddForce( ATVMesh->GetForwardVector() * InputForward * ControllerForce * ATVMesh->GetMass());
	}

	// Turn handling
	FRotator NewRotator = ATVMesh->GetRelativeRotation();
	ATVMesh->SetRelativeRotation( NewRotator.Add(0.0f, InputTurn * 1.0f, 0.0f), false, nullptr, ETeleportType::TeleportPhysics );

	if (TempDashTimer > 0.0f)
	{
		TempDashTimer = FMath::Max(0.0f, TempDashTimer - DeltaSeconds);
	}

	// DEBUG Drawing debug lines for fun and profit!
	if(DrawDebugVectorLines)
	{
		DrawDebugLine( GetWorld(),
		ATVMesh->GetComponentLocation(),
		ATVMesh->GetComponentLocation() + (ATVMesh->GetComponentVelocity() * DebugLineLength),
		DebugLineColor,
		false,
		DebugLineTime,
		0,
		DebugLineThickness );	
	}
}

void APlayerAllTerrainVehicle::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
	Super::SetupPlayerInputComponent(PlayerInputComponent);

	check(PlayerInputComponent != nullptr);

	Super::SetupPlayerInputComponent(PlayerInputComponent);

	InitializeDefaultPawnInputBindings();

	PlayerInputComponent->BindAxis("AllTerrainVehicle_MoveForward", this, &APlayerAllTerrainVehicle::MoveForward);
	PlayerInputComponent->BindAxis("AllTerrainVehicle_Turn", this, &APlayerAllTerrainVehicle::Turn);

	PlayerInputComponent->BindAction("AllTerrainVehicle_Jump", EInputEvent::IE_Pressed, this, &APlayerAllTerrainVehicle::Jump);
	PlayerInputComponent->BindAction("AllTerrainVehicle_Dash", EInputEvent::IE_Pressed, this, &APlayerAllTerrainVehicle::Dash);
}

void APlayerAllTerrainVehicle::Jump()
{
	// Only jump if we're in contact with something
	if(InContact)
	{
		// Add the impulse to the ATV to perform the jump
		ATVMesh->AddImpulse(FVector::UpVector * JumpForce);
	}
}

void APlayerAllTerrainVehicle::Dash()
{
	// Only dash if we're not dashing already.

	if (TempDashTimer == 0.0f)
	{
		// Only dash if we have an existing velocity vector to dash towards
		FVector velocity = ATVMesh->GetPhysicsLinearVelocity();

		if (velocity.Length() > 1.0f)
		{
			velocity.Normalize();
			ATVMesh->AddImpulse(velocity * DashForce * 1000.0f);

			// Set how long the dash lasts
			TempDashTimer = DashTime;
		}
	}
}
