import datetime
import sys
from ticks import *
from azure.storage import TableService
from blast_config import *
from azure_config import *

ts = TableService(azure_storage_account_name, azure_storage_account_key)
ts.create_table(history_table_name)

look_back_window_minutes = 15 # default

def calc_start_time(look_back_minutes):
    today = datetime.datetime.today()
    start_time = today - datetime.timedelta(minutes=look_back_minutes)
    return start_time

def show_recent_blast_logs(look_back_minutes):
    start_time = calc_start_time(look_back_minutes)
    start_tick = str(ticks_since_epoch(start_time))
    table_query = "PartitionKey ge '%s'" % start_tick
    entities = ts.query_entities(history_table_name, table_query)
    print("%d BLAST jobs completed in approx past %d minutes" % (len(entities), look_back_minutes))
    print("%s - %s - %s" % ("RowKey (input_num)", "Url", "UrlExpiration"))
    for entity in entities:
        print("%s - %s - %s" % (entity.RowKey, entity.Url, entity.UrlExpiration))

if (len(sys.argv) == 2):  # exactly 1 param, the look-back-window, in minutes
    look_back_window_minutes = int(sys.argv[1])
show_recent_blast_logs(look_back_window_minutes)

