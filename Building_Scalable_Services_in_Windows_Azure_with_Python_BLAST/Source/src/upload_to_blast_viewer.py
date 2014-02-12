import urllib, urllib2, urlparse, requests, uuid

upload_url = "http://bov.bioinfo.cas.unt.edu/cgi-bin/parseBlast.cgi"

def upload_to_blast_viewer(upload_file_path):
    print('loading from %s' % upload_file_path)
    data_to_upload = open(upload_file_path, 'r').read()
    #r = requests.post(upload_url, files = { 'uploadfile': ('blastout.99', data_to_upload) })
    r = requests.post(upload_url, files = { 'uploadfile': (data_to_upload) })
    hash = urlparse.parse_qs(r.url)["http://bov.bioinfo.cas.unt.edu/cgi-bin/viewhits.cgi?hash"][0]
    print('upload returned hash = %s' % hash)
    return hash

'''
print(data_to_upload)
print(r.text)
print(r.headers)
print(r.status_code)
print('hash = %s' %  urlparse.parse_qs(r.url)["http://bov.bioinfo.cas.unt.edu/cgi-bin/viewhits.cgi?hash"][0])
print('id = %s' %  uuid.uuid4().hex)
'''
