import imaplib
import email
from email.header import decode_header
from datetime import datetime
import os

# account credentials
username = "EMAIL@gmail.com"
password = "PASSWORD" #almost left my password here...!

# create an IMAP4 class with SSL 
imap = imaplib.IMAP4_SSL("imap.gmail.com")
# authenticate
imap.login(username, password)

status, messages = imap.select("INBOX")

# total number of emails
messages = int(messages[0])

today = ('Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun')[datetime.today().weekday()]
found = False
print('\nChecking for new work scanned today ({0}):\n'.format(today))
for i in reversed(range(messages)):
    
    # fetch the email message by ID
    res, msg = imap.fetch(str(i), "(RFC822)")
    for response in msg:
        if isinstance(response, tuple):
            msg = email.message_from_bytes(response[1])

            if today not in msg['Date']:
                break
                
            if msg['From'] == 'PRINTER@PROVIDER':
                for part in msg.walk():

                    filename = part.get_filename()
                    saved = open('PATH_TO/HPSCAN_SAVED.txt','r').read().split('\n')
                    
                    if filename and filename not in saved:
                        open('PATH_TO/HPSCAN_SAVED.txt','a').write(str(filename)+'\n')
                        filename = '(' + str(i) + ') ' + ' '.join(msg['Date'].split(' ')[1:4]).replace(':','.') + '.pdf'

                        # download attachment and save it
                        open('PATH_TO/'+filename, "wb").write(part.get_payload(decode=True))
                        print('Saving PDF:',filename)
                        found = True
    else: continue
    break
if not found: print('No new work found for today')

# close the connection and logout
imap.close()
imap.logout()


