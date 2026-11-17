using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisionClient.Models;

namespace VisionClient.Services;

public interface IVisionService
{
    Task<InspectionResult> GrabAndInspectAsync(int width, int height, CancellationToken ct = default);
}
