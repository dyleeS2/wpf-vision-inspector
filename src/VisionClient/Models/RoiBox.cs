using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VisionClient.Models;

public partial class RoiBox : ObservableObject
{
    [ObservableProperty]
    private double _x = 200;

    [ObservableProperty]
    private double _y = 150;

    [ObservableProperty]
    private double _width = 880;

    [ObservableProperty]
    private double _height = 720;
}
