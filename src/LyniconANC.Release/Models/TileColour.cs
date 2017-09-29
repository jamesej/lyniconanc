using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LyniconANC.Release.Models
{
    public enum TileColour
    {
        White,
        Cream,
        Ivory,
        Champagne,
        [Display(Name = "Sunshine Yellow")]
        SunshineYellow,
        [Display(Name = "Light Blue")]
        LightBlue,
        [Display(Name = "Dusty Blue")]
        DustyBlue,
        [Display(Name = "Dark Blue")]
        DarkBlue,
        [Display(Name = "Blue Black")]
        BlueBlack,
        Black,
        [Display(Name = "Pale Green")]
        PaleGreen,
        [Display(Name = "Moss Green")]
        MossGreen,
        Orange,
        [Display(Name = "Pink Red")]
        PinkRed,
        Crimson,
        [Display(Name = "Dark Red")]
        DarkRed,
        [Display(Name = "Deep Red")]
        DeepRed,
        [Display(Name = "Red Brown")]
        RedBrown,
        Brown,
        Beige,
        Tope,
        [Display(Name = "Black / White")]
        Black_White
    }
}
