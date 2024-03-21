using Godot;
using System;

public partial class display_name : TextEdit
{
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        //Text is the string value inside the textbox
        if (Text.Length >= 12)
        {
            //removes values outside of 12 character limit
            Text = Text.Substring(0, 11);
        }
    }
}
