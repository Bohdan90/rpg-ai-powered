using System;
using System.Collections.Generic;
namespace RPG.Core
{
    // §16: one rationally rounded sample per Chebyshev step; obstruction follows
    // the straight centerline, NOT the polyline through those rounded samples.
    internal static class FireStreamGeometry
    {
        struct Fraction : IComparable<Fraction>
        {
            public long N,D;
            public Fraction(long n,long d){if(d<0){n=-n;d=-d;}N=n;D=d;}
            public int CompareTo(Fraction b)=>(N*b.D).CompareTo(b.N*D);
        }
        static int RoundAway(int n,int d)=>Math.Sign(n)*((2*Math.Abs(n)+d)/(2*d));
        internal static List<GridPosition> Candidates(GridPosition c,GridPosition aim)
        {
            int dx=aim.X-c.X,dy=aim.Y-c.Y,m=Math.Max(Math.Abs(dx),Math.Abs(dy));var result=new List<GridPosition>();
            if(m<1||m>3)return result;
            for(int k=1;k<=3;k++)result.Add(new GridPosition(c.X+RoundAway(k*dx,m),c.Y+RoundAway(k*dy,m)));
            return result;
        }
        static bool Axis(int direction,int lo,int hi,ref Fraction entry,ref Fraction exit)
        {
            if(direction==0)return lo<0&&hi>0;
            var a=new Fraction(direction>0?lo:hi,2*direction);var b=new Fraction(direction>0?hi:lo,2*direction);
            if(a.CompareTo(entry)>0)entry=a;if(b.CompareTo(exit)<0)exit=b;
            return entry.CompareTo(exit)<0;
        }
        static bool Interior(GridPosition origin,int dx,int dy,GridPosition cell,Fraction limit,out Fraction entry)
        {
            entry=new Fraction(0,1);var exit=limit;
            return Axis(dx,2*(cell.X-origin.X)-1,2*(cell.X-origin.X)+1,ref entry,ref exit)
                &&Axis(dy,2*(cell.Y-origin.Y)-1,2*(cell.Y-origin.Y)+1,ref entry,ref exit);
        }
        internal static List<GridPosition> Cells(Battlefield board,GridPosition c,GridPosition aim)
        {
            var candidates=Candidates(c,aim);var result=new List<GridPosition>();if(candidates.Count==0)return result;
            int dx=aim.X-c.X,dy=aim.Y-c.Y,m=Math.Max(Math.Abs(dx),Math.Abs(dy));
            var limit=new Fraction(7,2*m);var stop=limit; // outer boundary of ring 3
            // Bounded 9x9 neighborhood includes map-edge exterior and omitted raster cells.
            for(int x=c.X-4;x<=c.X+4;x++)for(int y=c.Y-4;y<=c.Y+4;y++) {
                var cell=new GridPosition(x,y);
                if(!board.IsWalkable(cell)&&Interior(c,dx,dy,cell,limit,out var entry)&&entry.CompareTo(stop)<0)stop=entry;
            }
            if(dx!=0&&dy!=0)for(int x=c.X-4;x<c.X+4;x++)for(int y=c.Y-4;y<c.Y+4;y++) {
                int vx=2*(x-c.X)+1,vy=2*(y-c.Y)+1;
                if(vx*dy!=vy*dx)continue;var t=new Fraction(vx,2*dx);
                if(t.N<=0||t.CompareTo(stop)>=0)continue;
                bool sealedCorner=board.IsSolid(new GridPosition(x,y))&&board.IsSolid(new GridPosition(x+1,y+1))
                    ||board.IsSolid(new GridPosition(x+1,y))&&board.IsSolid(new GridPosition(x,y+1));
                if(sealedCorner)stop=t;
            }
            foreach(var cell in candidates)if(board.IsWalkable(cell)&&Interior(c,dx,dy,cell,limit,out var entry)&&entry.CompareTo(stop)<0)result.Add(cell);
            return result;
        }
    }
}
