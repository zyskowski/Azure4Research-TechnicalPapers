import datetime
import json
import time
from azure_config import *
from blast_config import *

from download_blast_database import make_sure_blast_database_is_downloaded
from run_blast_command import run_blast
from upload_to_blast_viewer import upload_to_blast_viewer
from get_next_blast_request import *
from log_blast_result import *

sleep_seconds = 60*5 # 5 minutes
# sleep_seconds = 5 # for testing & debugging only
timeout_seconds = 60*15 # 15 minutes

def now():
   return datetime.datetime.utcnow().strftime("%Y-%m-%d %H:%M:%S")

make_sure_blast_database_is_downloaded()

while True:
    try:
        print('About to access queue (@ %s)...' % now())
        msg = get_next_blast_request(timeout_seconds)
        print('get_next_blast_request returned (@ %s)' % now())
        if msg.body == None:
            print("No message returned (timeout)")
            print("Sleeping for %d seconds" % sleep_seconds)
            time.sleep(sleep_seconds)
        else:
            print("Message body = %s" % msg.body)
            blast_job = json.loads(msg.body)
            input_file = blast_job['InputFile']
            if (not input_file is None):
                print('run_blast(%s)' % input_file)
                output_file_path = run_blast(input_file)
                print('uploading "%s" to blast viewer' % output_file_path)
                hash = upload_to_blast_viewer(output_file_path)
                url = 'http://bov.bioinfo.cas.unt.edu/cgi-bin/viewhits.cgi?hash=%s' % hash
                print('url = %s' % url)
                blast_job['Hash'] = hash
                record_blast_job(blast_job) # so it shows up in BLAST.NET UI
                log_blast_result(input_file, url, blast_job['Id'])
                mark_blast_request_completed(msg)
    except:
        print
        print "EXCEPTION WAS RAISED... something didn't work that time.. trying again"
        print

