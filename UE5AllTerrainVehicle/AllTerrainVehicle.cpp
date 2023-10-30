// Sample C++ code from UE5 vehicle project by Ian Snyder


#include "AllTerrainVehicle.h"

// Sets default values
AAllTerrainVehicle::AAllTerrainVehicle()
{
 	// Set this pawn to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	ATVMesh = CreateDefaultSubobject<USkeletalMeshComponent>(TEXT("ATVMesh"));

	ATVMesh->SetSimulatePhysics(true);

	SetRootComponent(ATVMesh);
}

// Called when the game starts or when spawned
void AAllTerrainVehicle::BeginPlay()
{
	Super::BeginPlay();

	InitialLocation = ATVMesh->GetComponentLocation();

	ATVMesh->SetLinearDamping(0.5f);
	ATVMesh->SetAngularDamping(0.5f);
}

// Called every frame
void AAllTerrainVehicle::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

}

// Called to bind functionality to input
void AAllTerrainVehicle::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
	Super::SetupPlayerInputComponent(PlayerInputComponent);

	InContact = false;
}

