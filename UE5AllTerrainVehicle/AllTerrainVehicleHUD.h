// Sample C++ code from UE5 vehicle project by Ian Snyder

#pragma once

#include "CoreMinimal.h"
#include "DebugHUD.h"
#include "AllTerrainVehicleHUD.generated.h"

/**
 * 
 */
UCLASS()
class AAllTerrainVehicleHUD : public ADebugHUD
{
	GENERATED_BODY()

protected:

	// Draw the HUD
	virtual void DrawHUD() override;
};
