// Sample C++ code from UE5 vehicle project by Ian Snyder

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/HUD.h"
#include "Engine/Canvas.h"
#include "CanvasItem.h"
#include "DebugHUD.generated.h"

UCLASS()
class ALLTERRAINTANK_API ADebugHUD : public AHUD
{
	GENERATED_BODY()

protected:

	// Construct the debugging HUD, mainly establishing a font to use for display.
	ADebugHUD();

	// Add a FText to the HUD for rendering.
	void AddText(const TCHAR* Title, const FText& Value)
	{
		RenderStatistic(Title, Value);
	}

	// Add a float to the HUD for rendering.
	void AddFloat(const TCHAR* Title, const float Value)
	{
		RenderStatistic(Title, FText::AsNumber(Value), (Value < 1.0f) ? FLinearColor::Red : FLinearColor::Green);
	}

	// Add an int32 to the HUD for rendering.
	void AddInt(const TCHAR* Title, const int32 Value)
	{
		RenderStatistic(Title, FText::AsNumber(Value));
	}

	// Add a bool to the HUD for rendering.
	void AddBool(const TCHAR* Title, const bool bValue)
	{
		RenderStatistic(Title, BoolToText(bValue), (bValue == false) ? FLinearColor::Red : FLinearColor::Green);
	}

	// Draw the HUD.
	virtual void DrawHUD() override
	{
		X = Y = 50.0f;
	}

	// The horizontal offset to render the statistic values at.
	float HorizontalOffset = 150.0f;

private:

	// Convert a TCHAR pointer to FText.
	static FText CStringToText(const TCHAR* Text)
	{
		return FText::FromString(Text);
	}

	// Convert a bool to FText.
	static FText BoolToText(const bool bValue)
	{
		return CStringToText((bValue == true) ? TEXT("true") : TEXT("false"));
	}

	// Render a statistic onto the HUD canvas.
	void RenderStatistic(const TCHAR* title, const FText& Value, const FLinearColor& ValueColor = FLinearColor::White)
	{
		FCanvasTextItem Item0(FVector2D(X, Y), CStringToText(title), MainFont, FLinearColor::White);
		Item0.EnableShadow(FLinearColor(0.0f, 0.0f, 0.0f));
		Canvas->DrawItem(Item0);
		FCanvasTextItem Item1(FVector2D(X + HorizontalOffset, Y), Value, MainFont, ValueColor);
		Item1.EnableShadow(FLinearColor(0.0f, 0.0f, 0.0f));
		Canvas->DrawItem(Item1);
		Y += LineHeight;
	}

	// Font used to render the debug information.
	UPROPERTY(Transient)
		UFont* MainFont = nullptr;

	// The current X coordinate.
	float X = 50.0f;

	// The current Y coordinate.
	float Y = 50.0f;

	// The line height to separate each HUD entry.
	float LineHeight = 16.0f;
};
