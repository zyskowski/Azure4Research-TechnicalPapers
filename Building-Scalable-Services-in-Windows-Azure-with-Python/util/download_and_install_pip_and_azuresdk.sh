echo "download_and_install_pip_and_azure_sdk.sh"
curl https://raw.github.com/pypa/pip/master/contrib/get-pip.py | sudo python
sudo /usr/local/bin/pip-2.7 install azure
ls /usr/local/lib/python2.7/dist-packages/

