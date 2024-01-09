using Godot;
using System;

/**
 * ------------------------------------------------------
 * This is a reusable method for controlling the press of a texturebutton
 * ------------------------------------------------------
 */
public partial class TextureButtonScript : TextureButton
{
    [Export] public TextureButton button;
    [Export] public Control scriptNode;
    [Export] public String callbackName;
    public override void _Ready()
    {
        button.Pressed += _OnButtonPressed;
    }
    private void _OnButtonPressed()
    {
        scriptNode.Call(callbackName);
    }
}
