using System;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Buildings;
using UnityEngine;
using UnityEngine.UI;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        public void Population()
        { Roster(); }
    }
}
