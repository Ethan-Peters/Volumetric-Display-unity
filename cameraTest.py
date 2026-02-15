import cv2
import mediapipe as mp
import math
import sys
import socket
import json

print("Python version:", sys.version)
print("OpenCV version:", cv2.__version__)

# --- UDP ---
UDP_IP = "127.0.0.1"
UDP_PORT = 5005
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

def send_eye_data(x, y, dist): 
    payload = {
        "x": x, 
        "y": y, 
        "d": dist
    }
    message = json.dumps(payload).encode("utf-8")
    sock.sendto(message, (UDP_IP, UDP_PORT))

# --- OpenCV ---
print("Attempting to open camera...")
cap = cv2.VideoCapture(0)

if not cap.isOpened():
    print("Error: Could not open camera")
    exit()

print("Camera opened successfully")

# Set camera properties
cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)

# Test frame
print("Attempting to read first frame...")
ret, test_frame = cap.read()
print(f"First frame read result: ret={ret}, frame shape={test_frame.shape if ret else 'None'}")

if not ret:
    print("ERROR: Cannot read from camera!")
    cap.release()
    exit()

# --- MediaPipe with static_image_mode workaround ---
print("Initializing MediaPipe Face Mesh...")
mp_face_mesh = mp.solutions.face_mesh

# Try with static_image_mode=False and different settings
face_mesh = mp_face_mesh.FaceMesh(
    static_image_mode=False,
    max_num_faces=1,
    refine_landmarks=True,
    min_detection_confidence=0.5,
    min_tracking_confidence=0.5
)

print("MediaPipe initialized successfully!")

print("Creating window...")
cv2.namedWindow("Eye Tracker", cv2.WINDOW_NORMAL)
print("Window created")

print("Starting main loop... Press ESC to exit")

frame_count = 0
try:
    while True:
        ret, frame = cap.read()
        
        if not ret:
            print(f"Error: Can't receive frame from camera at frame {frame_count}")
            break

        frame_count += 1
        if frame_count == 1 or frame_count % 30 == 0:
            print(f"Processing frame {frame_count}")

        # Flip frame horizontally for mirror view (optional but nice)
        frame = cv2.flip(frame, 1)

        # MediaPipe expects RGB
        rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        
        # Make image not writeable to improve performance
        rgb.flags.writeable = False
        results = face_mesh.process(rgb)
        rgb.flags.writeable = True

        if results.multi_face_landmarks:
            face = results.multi_face_landmarks[0]
            h, w, _ = frame.shape

            # Iris landmarks
            left = face.landmark[468]
            right = face.landmark[473]

            lx, ly = int(left.x * w), int(left.y * h)
            rx, ry = int(right.x * w), int(right.y * h)

            # Distance
            dist = math.hypot(rx - lx, ry - ly)

            # Sending Data to UDP
            eye_x = ((left.x + right.x) / 2) - 0.5
            eye_y = ((left.y + right.y) / 2) - 0.5

            send_eye_data(eye_x, eye_y, dist)

            # Draw
            cv2.circle(frame, (lx, ly), 4, (0, 255, 0), -1)
            cv2.circle(frame, (rx, ry), 4, (0, 255, 0), -1)

            cv2.putText(
                frame,
                f" Eye dist: {dist:.1f}px   Position: {eye_x:.1f} {eye_y:.1f} ",
                (30, 40),
                cv2.FONT_HERSHEY_SIMPLEX,
                0.8,
                (255, 0, 0),
                2
            )
        else:
            cv2.putText(
                frame,
                "No face detected",
                (30, 40),
                cv2.FONT_HERSHEY_SIMPLEX,
                0.8,
                (0, 0, 255),
                2
            )

        cv2.imshow("Eye Tracker", frame)

        key = cv2.waitKey(1) & 0xFF
        if key == 27:
            print("ESC pressed, exiting...")
            break
        
        if frame_count == 1:
            print("First frame displayed successfully!")

except KeyboardInterrupt:
    print("Interrupted by user")
except Exception as e:
    print(f"Error occurred: {e}")
    import traceback
    traceback.print_exc()

print("Cleaning up...")
face_mesh.close()
cap.release()
cv2.destroyAllWindows()
print("Done")