// Sample C++ code from UE5 vehicle project by Ian Snyder


#include "AllTerrainVehicleHUD.h"
#include "PlayerAllTerrainVehicle.h"

void AAllTerrainVehicleHUD::DrawHUD()
{
	Super::DrawHUD();

	const APlayerAllTerrainVehicle* ATV = Cast<APlayerAllTerrainVehicle>(GetOwningPawn());

	if (ATV != nullptr)
	{
		AddBool(L"In contact", ATV->InContact);
		AddFloat(L"Velocity", ATV->GetVelocity().Size() / 100.0f);	// dividing by 100 converts from cm to meters so we're seeing meters/sec
		AddFloat(L"Angular Velocity", ATV->ATVMesh->GetPhysicsAngularVelocityInDegrees().Size());
		AddFloat(L"Dash timer", ATV->TempDashTimer);
		AddFloat(L"Input forward", ATV->InputForward);
		AddFloat(L"Input turn", ATV->InputTurn);
	}
}
