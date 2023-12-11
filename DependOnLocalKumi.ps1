$SOLUTION = ".\kumi.Server.sln"
$CSPROJ = @(
    ".\Kumi.Server\Kumi.Server.csproj"
)

$LOCAL_DEPENDENCIES = @(
    "..\kumi\Kumi.Game\Kumi.Game.csproj"
)

dotnet sln $SOLUTION add $LOCAL_DEPENDENCIES
dotnet add $CSPROJ reference $LOCAL_DEPENDENCIES