namespace Graph
{
    public class GraphSeriesEMAAveraging: GraphSeriesAveraging
    {
        double Value;
        public readonly double Periods;
        public readonly double Factor;

        public GraphSeriesEMAAveraging( double periods )
        {
            Periods = periods;
            Factor = 2 / ( Periods + 1 );
        }
        public override void NewBar() {}
        public override double Add( double newval )
        {
            Value = ( newval - Value ) * Factor + Value;
            return Value;
        }
    }
}