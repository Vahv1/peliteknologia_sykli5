using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//a class that has the tiletypes attributes
[System.Serializable]
public class TileType {

    public string name;
    public GameObject tileVisual; //visual of type

    public bool wall= false; //is the tiletype traversable

}
