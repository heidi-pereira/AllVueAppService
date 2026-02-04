using System.ComponentModel;

namespace OpenEnds.BackEnd.Model;

[DefaultValue(ClientUser)]
public enum OpenEndsRole
{
    ClientUser = 1,
    ClientAdmin = 2,
    SavantaUser = 3,
    SavantaAdmin = 4
}
