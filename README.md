# ![unb-api-net](Believe.Net/assets/logo_red.png) Believe.Net
A .Net wrapper for the UnbelievaBoat Discord bot API.

## Documentation
The [API documentation](https://unbelievaboat.com/api/docs) can be found at https://unbelievaboat.com/api/docs

### Installation  
Release builds are available from [Nuget](https://www.nuget.org/packages/Believe.Net)

## Example 
### Visual Basic
```vb
Dim UBClient As New UnbelievaClient("TOKEN")
Dim guildId = 551567205461131355
Dim userId = 551548379835006976
Dim userBalance = Await UBClient.GetUserBalanceAsync(guildId, userId)

Console.WriteLine(userBalance.Total)
```
### C#
```cs
UnbelievaClient UBClient = new UnbelievaClient("TOKEN");
var guildId = 551567205461131355;
var userId = 551548379835006976;
var userBalance = await UBClient.GetUserBalanceAsync(guildId, userId);

Console.WriteLine(userBalance.Total);
```
