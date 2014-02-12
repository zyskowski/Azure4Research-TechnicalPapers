import datetime
from ticks import *
from azure.storage import TableService
from blast_config import *
from azure_config import *

ts = TableService(azure_storage_account_name, azure_storage_account_key)
ts.create_table(history_table_name)

def calc_expiration():
    today = datetime.datetime.utcnow()
    expiration = today + datetime.timedelta(days=60)
    expiration = str(expiration) # could also treat as native date for Azure
    return expiration

def log_blast_result(input_file, url, id):
    partition_key = str(ticks_since_epoch())
    print('partition_key = %s' % partition_key)
    expiration_date = calc_expiration()
    ts.insert_entity(
        history_table_name,
        {
            'PartitionKey' : partition_key,
            'RowKey': input_file,
            'Id': id,
            'Url': url,
            'UrlExpiration': expiration_date
        }
    )

def record_blast_job(blast_job):
    # Since it is an error to reinsert with same PK/RK,
    # we use the "upsert" model
    ts.insert_or_merge_entity(
        job_table_name, 
        blast_job['Id'],
        blast_job['Id'],
        {
            'PartitionKey': blast_job['Id'],
            'RowKey': blast_job['Id'],
            'Id': blast_job['Id'],
            'Name': blast_job['Name'],
            'InputFile': blast_job['InputFile'],
            'State': 'OK',
            'LastMessage': 'Completed',
            'LastTimestamp': blast_job['LastTimestamp'],
            'Hash': blast_job['Hash']
        }
    )

