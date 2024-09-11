using System;

namespace WebFormsCore.Security;

[Flags]
public enum CspMode
{
    Uri = 1,
    Nonce = 2,
    Sha256 = 4,
}
