using System;
using System.Collections.Generic;

public class Name
{
    private List<string> _acceptedNames = new List<string>
    {
        "RightEmpty",
        "LeftEmpty",
        //Objects
        "Mug",
        //Interaction Right
        "RightGet_Mug",
        //Interaction Left
        "LeftGet_Mug",
    };

    private string _name = string.Empty;

    public Name(string tagName)
    {
        if (!_acceptedNames.Contains(tagName))
        {
            throw new ArgumentException("The detected object doesn't have a valid tag.");
        }
        _name = tagName;
    }

    public string Get()
    {
        return _name;
    }
}