using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionClient.Models;

public record InspectionResult(
    byte[] ImageBuffer,
    int Width,
    int Height,
    bool IsOk,
    double ProcessTimeMs,
    string Message
);
