import asyncio, aiohttp, random, sys

BASE_URL = "http://localhost"


async def test_all():
    uid = random.randint(10000, 99999)
    async with aiohttp.ClientSession() as s:

        async def post(url, data):
            return await s.post(f"{BASE_URL}{url}", json=data)

        async def get_bal():
            return (await (await s.get(f"{BASE_URL}/api/payments/accounts/{uid}")).json())['balance']


        async def wait_status(oid):
            for _ in range(20):
                orders = await (await s.get(f"{BASE_URL}/api/orders/orders")).json()
                st = next((o['status'] for o in orders if o['id'] == oid), None)
                if st in ['FINISHED', 'CANCELLED']: return st
                await asyncio.sleep(0.2)
            return "TIMEOUT"

        print(f"🚀 Start User: {uid}")

        # 1. SETUP: Создание + Пополнение на 100
        assert (await post("/api/payments/accounts", {"user_id": uid})).status == 200
        await post("/api/payments/accounts/topup", {"user_id": uid, "amount": 100})
        assert await get_bal() == 100.0
        print("✅ Account & Topup: OK")

        # 2. EDGE CASE: Покупка без денег (стоит 200, есть 100)
        res = await (await post("/api/orders/orders", {"user_id": uid, "amount": 200, "description": "NoMoney"})).json()
        assert await wait_status(res['id']) == 'CANCELLED'
        print("✅ Insufficient funds check: OK")

        # 3. RACE CONDITION & STRESS: 5 покупок по 100 (денег хватит только на 1)
        # Отправляем 5 запросов одновременно
        tasks = [post("/api/orders/orders", {"user_id": uid, "amount": 100, "description": f"Race {i}"}) for i in
                 range(5)]
        responses = await asyncio.gather(*tasks)
        order_ids = [(await r.json())['id'] for r in responses]

        # Ждем завершения всех
        statuses = [await wait_status(oid) for oid in order_ids]

        # Проверка: ровно 1 оплачен, 4 отменено
        finished = statuses.count('FINISHED')
        cancelled = statuses.count('CANCELLED')

        print(f"📊 Race Results: FINISHED={finished}, CANCELLED={cancelled}")
        if finished != 1 or cancelled != 4:
            raise Exception("❌ RACE CONDITION FAILED! Double spending or logic error.")

        # Финальный баланс должен быть 0
        assert await get_bal() == 0.0
        print("✅ Balance check: OK. Tests Passed!")


if __name__ == "__main__":
    if sys.platform == 'win32': asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())
    try:
        asyncio.run(test_all())
    except Exception as e:
        print(f"❌ ERROR: {e}")