// Sample C++ code from UE5 vehicle project by Ian Snyder

#include "DebugHUD.h"
#include "UObject/ConstructorHelpers.h"


// Construct the debugging HUD, mainly establishing a font to use for display

ADebugHUD::ADebugHUD()
{
	static ConstructorHelpers::FObjectFinder<UFont> Font(TEXT("/Engine/EngineFonts/Roboto"));

	MainFont = Font.Object;
}