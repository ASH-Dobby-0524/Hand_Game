import math

def detect_hand_orientation(hand_list, offset_deg=10):
    if not hand_list:
        return "none"

    hand = hand_list[0]

    wrist = hand.landmark[0]
    index_mcp = hand.landmark[5]

    dx = index_mcp.x - wrist.x
    dy = index_mcp.y - wrist.y

    angle = math.degrees(math.atan2(dy, dx))

    # 기준 오른쪽으로 이동
    angle += offset_deg



    # 오른쪽
    if -90 <= angle <= 90:
        return "right"

    # 왼쪽
    if angle >= 120 or angle <= -120:
        return "left"

    # 중앙
    return "middle"
