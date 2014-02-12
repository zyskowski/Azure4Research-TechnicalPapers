import datetime
import time
from random import randint
import sys

from azure_config import *
from get_next_blast_request import *

sleep_seconds = 60

if len(sys.argv) > 1:
    argv = sys.argv[1:] # strip off first entry which is program name
    for arg in argv:
        print('arg = ' + arg)
        input_num = int(arg)
        print("Sending input_num '%d' request to blast queue" % input_num)
        send_blast_request_by_number(input_num)
else: # run until interrupted, randomly selecting among valid inputs
    while True:
        input_num = randint(1,200)
        print("Sending input_num '%d' request to blast queue" % input_num)
        send_blast_request_by_number(input_num)
        print("Sleeping for %d seconds" % sleep_seconds)
        time.sleep(sleep_seconds)

