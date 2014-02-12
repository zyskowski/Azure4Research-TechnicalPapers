import datetime

def ticks_since_epoch(start_time_override = None):
    """
    Calculates number of Ticks since Jan 1, 0001 epoch. Uses current time unless another time is supplied.
    Mimics behavior of System.DateTime.UtcNow.Ticks from.NET with 10 million Ticks per second.
    http://msdn.microsoft.com/en-us/library/system.datetime.ticks.aspx
    @return: Number of Ticks since Jan 1, 0001 epoch (earliest date supported by Python datetime feature)
    """
    if (start_time_override is None):
        start_time = datetime.datetime.utcnow()
    else:
        start_time = start_time_override
    ticks_per_ms = 10000
    ms_per_second = 1000
    ticks_per_second = ticks_per_ms * ms_per_second
    span = start_time - datetime.datetime(1, 1, 1)
    ticks = int(span.total_seconds() * ticks_per_second)
    return ticks



