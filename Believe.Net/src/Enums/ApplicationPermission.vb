<Flags>
Public Enum ApplicationPermission As ULong
    ''' <summary>
    '''     View access (user balances, leaderboard, etc).
    ''' </summary>
    AccessEconomy = &H00_00_00_00
    ''' <summary>
    '''     Edit the server economy.
    ''' </summary>
    EditEconomy = &H00_00_00_01
    ''' <summary>
    '''     Edit server items and user inventories
    ''' </summary>
    EditInventory = &H00_00_00_02
End Enum
