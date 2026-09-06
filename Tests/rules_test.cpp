#include "Core/OdysseyRules.h"
#include "Core/OdysseyLocation.h"
#include <iostream>
#include <limits>
#include <random>
#include <cstdlib>

static int Checks=0;
static void Check(bool Condition, const char* Message)
{
    ++Checks;
    if (!Condition) { std::cerr << "FAIL: " << Message << '\n'; std::exit(1); }
}
int main()
{
    using namespace Odyssey;
    State S;
    Check(Valid(S),"new state validates");
    Check(Collect(S,0,0)==Result::Ok,"first collection succeeds");
    Check(S.Players[0].Acorns==9,"collection pays three acorns");
    Check(Collect(S,0,0)==Result::AlreadyCollected,"duplicate denied");
    Check(S.Players[0].Acorns==9,"duplicate does not pay twice");
    Check(S.Players[1].Memories==0 && S.Players[1].Acorns==6,"profiles isolated");
    Check(Collect(S,1,0)==Result::Ok,"same memory available to another player");
    Check(Build(S,0,24,Building::Observatory)==Result::Ok,"valid build");
    Check(Build(S,0,24,Building::Garden)==Result::Occupied,"occupied plot rejected");
    Check(Build(S,0,25,Building::Library)==Result::Insufficient,"insufficient funds rejected");
    Check(S.Players[0].Acorns==3 && S.Players[0].Plots[25]==Building::Empty,"failed build atomic");
    Check(Reclaim(S,0,24)==Result::Ok && S.Players[0].Acorns==9,"full reclaim refund");
    Check(Reclaim(S,0,24)==Result::Invalid && S.Players[0].Acorns==9,"no double refund");
    Check(Build(S,0,-1,Building::Garden)==Result::Invalid,"negative cell rejected");
    Check(Build(S,0,49,Building::Garden)==Result::Invalid,"outside grid rejected");
    Check(Build(S,4,0,Building::Garden)==Result::Invalid,"invalid player rejected");
    Check(Build(S,0,0,Building::Empty)==Result::Invalid,"empty building rejected");
    Check(Collect(S,0,12)==Result::Invalid && Collect(S,-1,0)==Result::Invalid,"bad artifact inputs rejected");
    Check(Valid(S),"valid after transaction sequence");
    State Community;
    for(int P=0;P<4;++P) for(int I=0;I<3;++I) Check(Contribute(Community,P)==Result::Ok,"community donation succeeds");
    Check(Community.CommunityAcorns==24 && Valid(Community),"four profiles complete shared goal");
    Check(Contribute(Community,0)==Result::Complete,"goal capped");
    State Forged=Community;
    Forged.Players[0].Acorns=42;
    Check(!Valid(Forged),"forged resource balance rejected");
    Forged=Community; Forged.Contributions[1]=0;
    Check(!Valid(Forged),"forged contribution totals rejected");
    Forged=State{}; Forged.Players[0].Appearance=static_cast<Style>(9);
    Check(!Valid(Forged),"invalid appearance rejected");
    Forged=State{}; Forged.Players[0].Plots[0]=static_cast<Building>(99);
    Check(!Valid(Forged),"invalid building rejected");
    State Complete;
    for(int I=0;I<12;++I) Collect(Complete,0,I);
    Check(Complete.Players[0].Acorns==42 && MemoryCount(Complete.Players[0])==12,"all memories collectible at home");
    Check(Build(Complete,0,48,Building::Hall)==Result::Ok && BuildingCount(Complete.Players[0])==1,"last cell usable");
    std::mt19937 Generator(1835);
    State Fuzz;
    for(int I=0;I<100000;++I)
    {
        int Player=int(Generator()%6)-1, Cell=int(Generator()%55)-3;
        switch(Generator()%4)
        {
            case 0: Collect(Fuzz,Player,int(Generator()%16)-2); break;
            case 1: Build(Fuzz,Player,Cell,static_cast<Building>(Generator()%7)); break;
            case 2: Reclaim(Fuzz,Player,Cell); break;
            default: Contribute(Fuzz,Player); break;
        }
        Check(Valid(Fuzz),"random transactions preserve resource conservation and bounds");
    }
    LocationFix Fix{42.0,-84.0,5,1,true};
    Check(CanCollectAtLocation(Fix,42,-84,25),"fresh accurate fix accepted");
    Fix.PermissionGranted=false;
    Check(!CanCollectAtLocation(Fix,42,-84,25),"no permission rejected");
    Fix.PermissionGranted=true; Fix.AgeSeconds=21;
    Check(!CanCollectAtLocation(Fix,42,-84,25),"stale location rejected");
    Fix.AgeSeconds=1; Fix.AccuracyMeters=31;
    Check(!CanCollectAtLocation(Fix,42,-84,50),"poor accuracy rejected");
    Fix.AccuracyMeters=5;
    Check(!CanCollectAtLocation(Fix,43,-84,50),"remote position rejected");
    Fix.Latitude=std::numeric_limits<double>::quiet_NaN();
    Check(!CanCollectAtLocation(Fix,42,-84,25),"NaN rejected");
    Check(std::abs(DistanceMeters(0,0,0,1)-111194.9266)<.1,"known geodesic distance");
    Check(DistanceMeters(0,179.999,0,-179.999)<223,"date line distance");
    std::cout << "PASS: " << Checks << " checks, including 100000 randomized transactions.\n";
}
