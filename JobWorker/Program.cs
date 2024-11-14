private static readonly ILog log = LogManager.GetLogger(typeof(JobBoss));

Log("\n\n _______                                   \n|_     _|.----..---.-..-----..-----..---.-.\n  |   |  |   _||  _  ||     ||  _  ||  _  |\n  |___|  |__|  |___._||__|__||___  ||___._|\n                             |_____|       \n\n");
string[] emojis = { "(•‿•)", "(づ \u25d5‿\u25d5 )づ", "( \u02d8\u25bd\u02d8)っ\u2668", "=\uff3e\u25cf \u22cf \u25cf\uff3e=", "（ΦωΦ）", "(\u272a\u3268\u272a)", "( ﾉ･o･ )ﾉ", "（〜^\u2207^ )〜", "~(\u2267ω\u2266)~","૮ \u00b4• ﻌ \u00b4• ა", "(\u02c3ᆺ\u02c2)", "(=\ud83d\udf66 \u0f1d \ud83d\udf66=)"};
SendNotifications("Tranga Started", emojis[Random.Shared.Next(0,emojis.Length-1)]);