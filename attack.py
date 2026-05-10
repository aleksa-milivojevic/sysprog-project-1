import requests
import threading

BASE_URL = "http://localhost:5134/"
FILES = [
    "f1.txt", "f2.pdf", "f3.jpg", "f4.zip", "f5.png",
    "f6.html", "f7.csv", "f8.json", "f9.mp3", "f10.docx"
]
REQNUM = 10

def send_req(url, file):
    try:
        full_url = f"{url}{file}"
        response = requests.get(full_url, timeout=10)
        print(f"File: {file} | Status: {response.status_code}")
    except Exception as e:
        print(f"Error: {file}: {e}")

threads = []

for file in FILES:
    for _ in range(REQNUM):
        t = threading.Thread(target=send_req, args=(BASE_URL, file))
        threads.append(t)
        t.start()

for t in threads:
    t.join()

print("\nSvi zahtevi su poslati")