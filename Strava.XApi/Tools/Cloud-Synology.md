sudo mkdir /mnt/sqlvaxr6gnm7lbpr6w
if [ ! -d "/etc/smbcredentials" ]; then
sudo mkdir /etc/smbcredentials
fi
if [ ! -f "/etc/smbcredentials/sqlvaxr6gnm7lbpr6w.cred" ]; then
    sudo bash -c 'echo "username=sqlvaxr6gnm7lbpr6w" >> /etc/smbcredentials/sqlvaxr6gnm7lbpr6w.cred'
    sudo bash -c 'echo "password=RREiFwXwNkF5UC3pRupQd4qaR3Uel56BLRUdYprb8OEUv4snNkUOpdIlZOWXNTcxmf3jxY18t6kYuGB2BdP7dw==" >> /etc/smbcredentials/sqlvaxr6gnm7lbpr6w.cred'
fi
sudo chmod 600 /etc/smbcredentials/sqlvaxr6gnm7lbpr6w.cred

sudo bash -c 'echo "//sqlvaxr6gnm7lbpr6w.file.core.windows.net/elp01share /mnt/sqlvaxr6gnm7lbpr6w cifs nofail,vers=3.0,credentials=/etc/smbcredentials/sqlvaxr6gnm7lbpr6w.cred,dir_mode=0777,file_mode=0777,serverino" >> /etc/fstab'
sudo mount -t cifs //sqlvaxr6gnm7lbpr6w.file.core.windows.net/elp01share /mnt/sqlvaxr6gnm7lbpr6w -o vers=3.0,credentials=/etc/smbcredentials/sqlvaxr6gnm7lbpr6w.cred,dir_mode=0777,file_mode=0777,serverino


rsync -av EricLouvard@lx-free12-vm.northeurope.cloudapp.azure.com:~/projects/strava-x-api/Prototype/maps/* /var/services/homes/Eric/projects/strava-x-api/Prototype/maps
rsync -av sqlvaxr6gnm7lbpr6w@sqlvaxr6gnm7lbpr6w.file.core.windows.net:/elp01share/* /var/services/homes/Eric/projects/strava-x-api/Prototype/elp01/

