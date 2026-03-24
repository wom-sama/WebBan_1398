import os
import re
import subprocess
import sys
import time
from pathlib import Path
from typing import Iterable
from urllib.parse import urljoin

import requests


ROOT = Path(r"m:\WebBan_1398")
PROJECT_DIR = ROOT / "WebBan_1398"
APP_EXE = PROJECT_DIR / "bin" / "Debug" / "net8.0" / "WebBan_1398.exe"
BASE_URL = "http://127.0.0.1:5055"
IMAGE_PATH = PROJECT_DIR / "wwwroot" / "Image" / "placeholder.png"
SQL_BASE = ["sqlcmd", "-S", r".\SQLEXPRESS", "-d", "DBWebBH", "-W", "-h", "-1", "-Q"]


def log(message: str) -> None:
    safe_message = message.encode("utf-8", errors="ignore").decode("utf-8")
    print(safe_message, flush=True)


def fail(message: str) -> None:
    raise RuntimeError(message)


def wait_for_server() -> None:
    deadline = time.time() + 45
    while time.time() < deadline:
        try:
            response = requests.get(f"{BASE_URL}/", timeout=3)
            if response.ok:
                return
        except requests.RequestException:
            time.sleep(0.5)
    fail("Web app did not become ready in time.")


def sql(query: str) -> str:
    result = subprocess.run(
        SQL_BASE + [f"SET NOCOUNT ON; {query}"],
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="ignore",
        check=True,
    )
    return result.stdout.strip()


def sql_scalar(query: str) -> str:
    output = sql(query)
    if not output:
        fail(f"SQL query returned no rows: {query}")
    return output.splitlines()[-1].strip()


def get_token(html: str) -> str:
    match = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', html)
    if not match:
        fail("Could not find anti-forgery token.")
    return match.group(1)


def ensure_contains(text: str, expected: str, context: str) -> None:
    if expected not in text:
        fail(f"Expected to find '{expected}' in {context}.")


def extract_messages(html: str) -> list[str]:
    raw_messages = re.findall(r'<(?:div|span)[^>]*class="[^"]*text-danger[^"]*"[^>]*>(.*?)</(?:div|span)>', html, re.S)
    messages: list[str] = []
    for raw_message in raw_messages:
        cleaned = re.sub(r"<[^>]+>", " ", raw_message)
        cleaned = re.sub(r"\s+", " ", cleaned).strip()
        if cleaned and cleaned not in messages:
            messages.append(cleaned)
    return messages


def submit_form(
    session: requests.Session,
    url: str,
    data: dict[str, str],
    files: Iterable[tuple[str, tuple[str, object, str]]] | None = None,
) -> requests.Response:
    response = session.get(url, timeout=20)
    response.raise_for_status()
    payload = {"__RequestVerificationToken": get_token(response.text)}
    payload.update(data)
    return session.post(url, data=payload, files=files, timeout=30, allow_redirects=True)


def assign_admin_role(email: str) -> None:
    safe_email = email.replace("'", "''")
    query = f"""
DECLARE @UserId nvarchar(450) = (SELECT Id FROM AspNetUsers WHERE Email = '{safe_email}');
DECLARE @RoleId nvarchar(450) = (SELECT Id FROM AspNetRoles WHERE Name = 'Admin');
IF @UserId IS NULL
    THROW 50000, 'Test user was not created.', 1;
IF @RoleId IS NULL
    THROW 50000, 'Admin role was not found.', 1;
IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
    INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@UserId, @RoleId);
"""
    sql(query)


def main() -> int:
    email = f"codex.admin.{int(time.time())}@example.com"
    password = "Admin123!"
    marker = int(time.time())
    category_name = f"Danh muc test {marker}"
    product_name = f"San pham test {marker}"
    shipping_address = f"So {marker} Duong Kiem Thu, Quan 1, TP.HCM"
    notes = "Don hang duoc tao tu smoke test."

    server = subprocess.Popen(
        [str(APP_EXE), "--urls", BASE_URL],
        cwd=str(PROJECT_DIR),
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )

    try:
        log("1. Cho web app khoi dong...")
        wait_for_server()

        public_session = requests.Session()
        admin_session = requests.Session()

        log("2. Dang ky tai khoan test...")
        register_response = submit_form(
            public_session,
            f"{BASE_URL}/Identity/Account/Register",
            {
                "Input.FullName": "Codex Admin Test",
                "Input.Age": "26",
                "Input.Address": "123 Test Street",
                "Input.Email": email,
                "Input.Password": password,
                "Input.ConfirmPassword": password,
            },
        )
        register_response.raise_for_status()
        ensure_contains(register_response.text, "logoutForm", "register response")

        log("3. Gan quyen Admin cho tai khoan test...")
        assign_admin_role(email)

        log("4. Dang nhap lai bang quyen Admin...")
        login_response = submit_form(
            admin_session,
            f"{BASE_URL}/Identity/Account/Login?ReturnUrl=%2FAdmin",
            {
                "Input.Email": email,
                "Input.Password": password,
                "Input.RememberMe": "false",
            },
        )
        login_response.raise_for_status()
        ensure_contains(login_response.url, "/Admin", "login redirect URL")

        admin_home = admin_session.get(f"{BASE_URL}/Admin", timeout=20)
        admin_home.raise_for_status()
        ensure_contains(admin_home.text, "/Admin/Product", "admin dashboard")

        log("5. Tao danh muc moi tu Area/Admin...")
        category_response = submit_form(
            admin_session,
            f"{BASE_URL}/Admin/Category/Add",
            {"Name": category_name},
        )
        category_response.raise_for_status()
        if category_response.url.endswith("/Admin/Category/Add"):
            fail(f"Category form stayed on Add page. Validation: {extract_messages(category_response.text)}")
        ensure_contains(category_response.text, category_name, "category index page")
        category_id = sql_scalar(f"SELECT TOP 1 Id FROM Categories WHERE Name = N'{category_name}' ORDER BY Id DESC;")

        log("6. Tao san pham moi tu Area/Admin...")
        with IMAGE_PATH.open("rb") as image_file, IMAGE_PATH.open("rb") as gallery_file:
            product_response = submit_form(
                admin_session,
                f"{BASE_URL}/Admin/Product/Add",
                {
                    "Name": product_name,
                    "Price": "123000",
                    "Description": "San pham duoc tao de kiem thu luong mua hang.",
                    "CategoryId": category_id,
                },
                files=[
                    ("imageUrl", ("placeholder.png", image_file, "image/png")),
                    ("imageUrls", ("placeholder.png", gallery_file, "image/png")),
                ],
            )
        product_response.raise_for_status()
        if product_response.url.endswith("/Admin/Product/Add"):
            fail(f"Product form stayed on Add page. Validation: {extract_messages(product_response.text)}")
        ensure_contains(product_response.text, product_name, "product index page")
        product_id = sql_scalar(f"SELECT TOP 1 Id FROM Products WHERE Name = N'{product_name}' ORDER BY Id DESC;")

        log("7. Kiem tra trang chi tiet va them vao gio hang...")
        product_page = admin_session.get(f"{BASE_URL}/Product/Display/{product_id}", timeout=20)
        product_page.raise_for_status()
        ensure_contains(product_page.text, product_name, "public product detail")

        add_to_cart = admin_session.get(
            f"{BASE_URL}/ShoppingCart/AddToCart?productId={product_id}&quantity=2&returnUrl=%2FProduct%2FDisplay%2F{product_id}",
            timeout=20,
            allow_redirects=True,
        )
        add_to_cart.raise_for_status()
        ensure_contains(add_to_cart.text, product_name, "add-to-cart redirect page")

        cart_page = admin_session.get(f"{BASE_URL}/ShoppingCart", timeout=20)
        cart_page.raise_for_status()
        ensure_contains(cart_page.text, product_name, "shopping cart")

        log("8. Dat hang va xac nhan don hang...")
        checkout_response = submit_form(
            admin_session,
            f"{BASE_URL}/ShoppingCart/Checkout",
            {
                "ShippingAddress": shipping_address,
                "Notes": notes,
            },
        )
        checkout_response.raise_for_status()
        ensure_contains(checkout_response.text, "order-success-card", "order completed page")

        order_id = sql_scalar(
            f"SELECT TOP 1 Id FROM Orders WHERE UserId = (SELECT Id FROM AspNetUsers WHERE Email = '{email}') ORDER BY Id DESC;"
        )

        admin_order_index = admin_session.get(f"{BASE_URL}/Admin/Order", timeout=20)
        admin_order_index.raise_for_status()
        ensure_contains(admin_order_index.text, f"#{order_id}", "admin order list")

        admin_order_detail = admin_session.get(f"{BASE_URL}/Admin/Order/Details/{order_id}", timeout=20)
        admin_order_detail.raise_for_status()
        ensure_contains(admin_order_detail.text, product_name, "admin order details")

        print()
        print("SMOKE TEST OK")
        print(f"Admin test account: {email}")
        print(f"Password: {password}")
        print(f"Created category: {category_name} (Id={category_id})")
        print(f"Created product: {product_name} (Id={product_id})")
        print(f"Created order: #{order_id}")
        return 0
    finally:
        server.terminate()
        try:
            server.wait(timeout=10)
        except subprocess.TimeoutExpired:
            server.kill()


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f"SMOKE TEST FAILED: {exc}", file=sys.stderr)
        raise
