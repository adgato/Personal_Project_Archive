using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music_Theory
{
    public class Degree
    {
        public readonly int degree;
        public readonly Accidental accidental;

        public enum Accidental
        {
            flat = -1,
            natural = 0,
            sharp = 1
        }

        public static readonly Degree I = 0;
        public static readonly Degree II = 1;
        public static readonly Degree III = 2;
        public static readonly Degree IV = 3;
        public static readonly Degree V = 4;
        public static readonly Degree VI = 5;
        public static readonly Degree VII = 6;
        public static readonly Degree VIII = 7;
        public static readonly Degree IX = 8;
        public static readonly Degree X = 9;
        public static readonly Degree XI = 10;
        public static readonly Degree XII = 11;

        public Degree(int degree, Accidental accidental)
        {
            this.degree = degree;
            this.accidental = accidental;
        }

        public static implicit operator Degree(int degree) => new Natural(degree);
        public static Degree operator +(Degree a, Degree b)
        {
            int degree = a.degree + b.degree + ((int)a.accidental + (int)b.accidental) / 2;
            Accidental accidental = (Accidental)(((int)a.accidental + (int)b.accidental) % 2);
            return new Degree(degree, accidental);
        }
        public static Degree operator -(Degree a, Degree b)
        {
            int degree = a.degree - b.degree + ((int)a.accidental - (int)b.accidental) / 2;
            Accidental accidental = (Accidental)(((int)a.accidental - (int)b.accidental) % 2);
            return new Degree(degree, accidental);
        }
    }
    public class Flat : Degree
    {
        public Flat(int degree) : base(degree, Accidental.flat) { }

        public static readonly Flat _1 = new Flat(0);
        public static readonly Flat _2 = new Flat(1);
        public static readonly Flat _3 = new Flat(2);
        public static readonly Flat _4 = new Flat(3);
        public static readonly Flat _5 = new Flat(4);
        public static readonly Flat _6 = new Flat(5);
        public static readonly Flat _7 = new Flat(6);
        public static readonly Flat _8 = new Flat(7);
        public static readonly Flat _9 = new Flat(8);
        public static readonly Flat _10 = new Flat(9);
        public static readonly Flat _11 = new Flat(10);
        public static readonly Flat _12 = new Flat(11);

        public static readonly new Flat I = _1;
        public static readonly new Flat II = _2; 
        public static readonly new Flat III = _3; 
        public static readonly new Flat IV = _4; 
        public static readonly new Flat V = _5; 
        public static readonly new Flat VI = _6;
        public static readonly new Flat VII = _7;
        public static readonly new Flat VIII = _8;
        public static readonly new Flat IX = _9;
        public static readonly new Flat X = _10;
        public static readonly new Flat XI = _11;
        public static readonly new Flat XII = _12;
    }
    public class Natural : Degree
    {
        public Natural(int degree) : base(degree, Accidental.natural) { }

        public static readonly Natural _1 = new Natural(0);
        public static readonly Natural _2 = new Natural(1);
        public static readonly Natural _3 = new Natural(2);
        public static readonly Natural _4 = new Natural(3);
        public static readonly Natural _5 = new Natural(4);
        public static readonly Natural _6 = new Natural(5);
        public static readonly Natural _7 = new Natural(6);
        public static readonly Natural _8 = new Natural(7);
        public static readonly Natural _9 = new Natural(8);
        public static readonly Natural _10 = new Natural(9);
        public static readonly Natural _11 = new Natural(10);
        public static readonly Natural _12 = new Natural(11);

        public static readonly new Natural I = _1;
        public static readonly new Natural II = _2;
        public static readonly new Natural III = _3;
        public static readonly new Natural IV = _4;
        public static readonly new Natural V = _5;
        public static readonly new Natural VI = _6;
        public static readonly new Natural VII = _7;
        public static readonly new Natural VIII = _8;
        public static readonly new Natural IX = _9;
        public static readonly new Natural X = _10;
        public static readonly new Natural XI = _11;
        public static readonly new Natural XII = _12;

        public static explicit operator int(Natural natural) => natural.degree;
    }
    public class Sharp : Degree
    {
        public Sharp(int degree) : base(degree, Accidental.sharp) { }

        public static readonly Sharp _1 = new Sharp(0);
        public static readonly Sharp _2 = new Sharp(1);
        public static readonly Sharp _3 = new Sharp(2);
        public static readonly Sharp _4 = new Sharp(3);
        public static readonly Sharp _5 = new Sharp(4);
        public static readonly Sharp _6 = new Sharp(5);
        public static readonly Sharp _7 = new Sharp(6);
        public static readonly Sharp _8 = new Sharp(7);
        public static readonly Sharp _9 = new Sharp(8);
        public static readonly Sharp _10 = new Sharp(9);
        public static readonly Sharp _11 = new Sharp(10);
        public static readonly Sharp _12 = new Sharp(11);

        public static readonly new Sharp I = _1;
        public static readonly new Sharp II = _2;
        public static readonly new Sharp III = _3;
        public static readonly new Sharp IV = _4;
        public static readonly new Sharp V = _5;
        public static readonly new Sharp VI = _6;
        public static readonly new Sharp VII = _7;
        public static readonly new Sharp VIII = _8;
        public static readonly new Sharp IX = _9;
        public static readonly new Sharp X = _10;
        public static readonly new Sharp XI = _11;
        public static readonly new Sharp XII = _12;
    }

    /// <summary>
    /// Every possible musical scale, where the scale strictly increases, and ends exactly one octave higher than the tonic.
    /// Scales that are different modes of scales already in this enumeration are excluded.
    /// </summary>
    public enum Progression
    {
        s111111111111,
        s21111111111,
        s3111111111,
        s2111121111,
        s2111211111,
        s2112111111,
        s2121111111,
        s2211111111,
        s411111111,
        s211111131,
        s211111311,
        s211113111,
        s211131111,
        s211311111,
        s213111111,
        s231111111,
        s321111111,
        s211211211,
        s212111211,
        s212112111,
        s212121111,
        s221111121,
        s221111211,
        s221112111,
        s221121111,
        s221211111,
        s222111111,
        s51111111,
        s21111141,
        s21111411,
        s21114111,
        s21141111,
        s21411111,
        s24111111,
        s42111111,
        s31113111,
        s31131111,
        s31311111,
        s33111111,
        s21121131,
        s21121311,
        s21123111,
        s21132111,
        s21211131,
        s21211311,
        s21213111,
        s21231111,
        s21312111,
        s21321111,
        s22111131,
        s22111311,
        s22113111,
        s22131111,
        s22311111,
        s23112111,
        s23121111,
        s23211111,
        s32112111,
        s32121111,
        s32211111,
        s21212121,
        s22112121,
        s22112211,
        s22121121,
        s22121211,
        s22122111,
        s22211121,
        s22211211,
        s22212111,
        s22221111,
        s6111111,
        s2111151,
        s2111511,
        s2115111,
        s2151111,
        s2511111,
        s5211111,
        s3111141,
        s3111411,
        s3114111,
        s3141111,
        s3411111,
        s4311111,
        s2121141,
        s2121411,
        s2124111,
        s2141211,
        s2142111,
        s2211141,
        s2211411,
        s2214111,
        s2241111,
        s2411211,
        s2412111,
        s2421111,
        s4211211,
        s4212111,
        s4221111,
        s2111331,
        s2113131,
        s2113311,
        s2131131,
        s2131311,
        s2133111,
        s2311131,
        s2311311,
        s2313111,
        s2331111,
        s3211131,
        s3211311,
        s3213111,
        s3231111,
        s3321111,
        s2211231,
        s2211321,
        s2212131,
        s2212311,
        s2213121,
        s2213211,
        s2221131,
        s2221311,
        s2223111,
        s2231121,
        s2231211,
        s2232111,
        s2312121,
        s2321121,
        s2321211,
        s2322111,
        s3212121,
        s3221121,
        s3221211,
        s3222111,
        s2221221,
        s2222121,
        s2222211,
        s711111,
        s211161,
        s211611,
        s216111,
        s261111,
        s621111,
        s311151,
        s311511,
        s315111,
        s351111,
        s531111,
        s411411,
        s414111,
        s441111,
        s212151,
        s212511,
        s215211,
        s221151,
        s221511,
        s225111,
        s251211,
        s252111,
        s521211,
        s522111,
        s211341,
        s211431,
        s213141,
        s213411,
        s214131,
        s214311,
        s231141,
        s231411,
        s234111,
        s241131,
        s241311,
        s243111,
        s321141,
        s321411,
        s324111,
        s342111,
        s421131,
        s421311,
        s423111,
        s432111,
        s313131,
        s331131,
        s331311,
        s333111,
        s221241,
        s221421,
        s222141,
        s222411,
        s224121,
        s224211,
        s242121,
        s242211,
        s422121,
        s422211,
        s221331,
        s223131,
        s223311,
        s231231,
        s232131,
        s232311,
        s233121,
        s233211,
        s321231,
        s321321,
        s322131,
        s322311,
        s323121,
        s323211,
        s332121,
        s332211,
        s222231,
        s222321,
        s223221,
        s232221,
        s322221,
        s222222,
        s81111,
        s21171,
        s21711,
        s27111,
        s72111,
        s31161,
        s31611,
        s36111,
        s63111,
        s41151,
        s41511,
        s45111,
        s54111,
        s22161,
        s22611,
        s26121,
        s26211,
        s62121,
        s62211,
        s21351,
        s21531,
        s23151,
        s23511,
        s25131,
        s25311,
        s32151,
        s32511,
        s35211,
        s52131,
        s52311,
        s53211,
        s21441,
        s24141,
        s24411,
        s42141,
        s42411,
        s44211,
        s33141,
        s33411,
        s34131,
        s34311,
        s43131,
        s43311,
        s22251,
        s22521,
        s25221,
        s52221,
        s22341,
        s22431,
        s23241,
        s23421,
        s24231,
        s24321,
        s32241,
        s32421,
        s34221,
        s42231,
        s42321,
        s43221,
        s23331,
        s32331,
        s33231,
        s33321,
        s42222,
        s32322,
        s33222,
        s9111,
        s2181,
        s2811,
        s8211,
        s3171,
        s3711,
        s7311,
        s4161,
        s4611,
        s6411,
        s5151,
        s5511,
        s2271,
        s2721,
        s7221,
        s2361,
        s2631,
        s3261,
        s3621,
        s6231,
        s6321,
        s2451,
        s2541,
        s4251,
        s4521,
        s5241,
        s5421,
        s3351,
        s3531,
        s5331,
        s3441,
        s4341,
        s4431,
        s6222,
        s3252,
        s3522,
        s5322,
        s4242,
        s4422,
        s3342,
        s3432,
        s4332,
        s3333,
        sA11,
        s291,
        s921,
        s381,
        s831,
        s471,
        s741,
        s561,
        s651,
        s822,
        s372,
        s732,
        s462,
        s642,
        s552,
        s633,
        s453,
        s543,
        s444,
        sB1,
        sA2,
        s93,
        s84,
        s75,
        s66,
        sC
    }
}
