using Verse;

namespace rimstocks;

public class FactionData : IExposable
{
    public int loan;
    public float loan_per;
    public float loan_raidMulti;
    public int loan_targetTick;
    public int loan_totalTick;

    public int loan_leftTick => loan_targetTick - Find.TickManager.TicksGame;
    public int loan_flowTick => loan_totalTick - loan_leftTick;

    public void ExposeData()
    {
        Scribe_Values.Look(ref loan, "loan");
        Scribe_Values.Look(ref loan_totalTick, "loan_totalTick");
        Scribe_Values.Look(ref loan_targetTick, "loan_targetTick");
        Scribe_Values.Look(ref loan_per, "loan_per");
        Scribe_Values.Look(ref loan_raidMulti, "loan_raidMulti");
    }

    public void clear()
    {
        loan = 0;
        loan_totalTick = 0;
        loan_targetTick = 0;
        loan_per = 0f;
        loan_raidMulti = 0f;
    }
}