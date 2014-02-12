import json
import uuid
import time
from azure.servicebus import * 
from blast_config import *
from azure_config import *
from ticks import *

sbs = ServiceBusService(service_bus_namespace,
                        service_bus_key,
                        'owner')

sbs.create_queue(request_queue_name)

peek_lock = True

def get_next_blast_request(timeout_seconds = 30):
    msg = sbs.receive_queue_message(request_queue_name, peek_lock, timeout_seconds)
    return msg

def mark_blast_request_completed(msg):
    if peek_lock:
        msg.delete()

def send_blast_request_by_number(input_num):
    input_name = blast_input_file_template % input_num
    send_blast_request(input_name)

def send_blast_request(input_name):
    msg = Message()

    data = {
            'Id': uuid.uuid4().hex,
            'Name': 'Python TEST %s (%s)' % (time.strftime("%Y-%m-%d-%H-%M-%S", time.localtime()), input_name),
            'InputFile': input_name,
            'Hash':"",
            'OutputFile': "",
            'State': "QUEUED",
            'LastMessage': "Queued",
            'InputFiles': "null",
            'LastTimestamp': ticks_since_epoch()
            }

    msg.body = json.dumps(data);
    sbs.send_queue_message(request_queue_name, msg)



